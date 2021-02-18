using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    internal class AjivaInstallPacker
    {
        private static readonly byte[] PackHeader = {(byte)'I', (byte)'N', (byte)'S', (byte)'T', (byte)'_', (byte)'P', (byte)'A', (byte)'C', (byte)'K'};
        private const string PackageExtension = ".inst_pack";

        private const int BufferLength = 4194304;

        public void Pack(string path, string name)
        {
            path = Path.GetFullPath(path);
            var @out = Path.Combine(path, "..", name + PackageExtension);
            if (!OpenFile(@out, out FileStream pack)) return;

            FileDescriptor MkFileDescriptor(FileInfo arg) => new(arg, arg.FullName.Substring(path.Length + 1), 0, arg.Length);

            var files = RecEnumerator(new(path)).Select(MkFileDescriptor).ToList();

            WritePack(pack, files);
            LogHelper.Log($"Finished: {@out}");
        }

        public class FileDescriptor
        {
            public FileInfo FileInfo { get; set; }
            public string RelPath { get; set; }
            public long Pos { get; set; }
            public long Length { get; set; }

            public FileDescriptor(FileInfo fileInfo, string relPath, long pos, long length)
            {
                FileInfo = fileInfo;
                RelPath = relPath;
                Pos = pos;
                Length = length;
            }

            public int FileDescriptorLength => sizeof(ushort) + RelPath.Length + sizeof(long) + sizeof(long);

            internal class WriteIt
            {
                public int Size;
                public byte[] Value;
            }

            public void WriteHeadTo(MemoryStream header)
            {
                header.Write(BitConverter.GetBytes((ushort)RelPath.Length));
                header.Write(Encoding.UTF8.GetBytes(RelPath));
                header.Write(BitConverter.GetBytes(Pos));
                header.Write(BitConverter.GetBytes(Length));
            }

            public static FileDescriptor ReadHeadFrom(MemoryStream header)
            {
                Span<byte> tmp = new byte[sizeof(ushort)];

                header.Read(tmp);
                tmp = new byte[BitConverter.ToInt16(tmp)]; //str length
                header.Read(tmp);
                var relPath = Encoding.UTF8.GetString(tmp);
                tmp = new byte[sizeof(long)];
                header.Read(tmp);
                var pos = BitConverter.ToInt64(tmp);
                header.Read(tmp);
                var length = BitConverter.ToInt64(tmp);
                return new(null!, relPath, pos, length);
            }
        }

        private void WritePack(FileStream pack, List<FileDescriptor> files)
        {
            var headBuffer = new byte[files.Sum(x => x.FileDescriptorLength)];
            var headMmStr = new MemoryStream(headBuffer);

            WriteHead(pack, new(files.Count, headBuffer.Length, "aaa", "vvv", "bbb", "ddd"));

            var ph = pack.Position;
            pack.Position = ph + headBuffer.Length;
            var p0 = pack.Position;

            foreach (var file in files)
            {
                file.Pos = pack.Position - p0;
                try
                {
                    WriteFile(file, pack);
                }
                catch (Exception e)
                {
                    LogHelper.Log($"Failed to add: {file.RelPath} ({e.Message})");
                    file.Length = 0;
                    pack.Position = p0 + file.Pos; //reset head
                    pack.SetLength(p0 + file.Pos); //remove allocation
                    //todo set some error bit?
                }

                file.WriteHeadTo(headMmStr);
            }
            Debug.Assert(headMmStr.Position == headMmStr.Length);

            pack.Flush();
            pack.Position = ph;
            headMmStr.WriteTo(pack);

            pack.Close();
        }

        private record PackHead(long FilesCount, int HeadLength, string Name, string Description, string Executable, string Arguments)
        {
            [JsonConstructor]
            public PackHead() : this(default!, default!, default!, default!, default!, default!)
            {
            }
        }

        private static void WriteHead(Stream pack, PackHead head)
        {
            pack.Write(PackHeader);

            WriteAsJson(pack, head);
        }

        private static PackHead ReadHead(Stream pack)
        {
            Span<byte> tmp = new byte[PackHeader.Length];

            pack.Read(tmp);
            if (!tmp.SequenceEqual(PackHeader))
            {
                pack.Close();
                throw new ArgumentException("File is not the right type!");
            }

            var obj = ReadJsonObjectAs<PackHead>(pack, out _);
            return obj!;
        }

        private void WriteFile(FileDescriptor file, Stream fs)
        {
            //todo remove from read this: WriteString(fName, fs);
            //todo s.o. var ln = file.Length;
            //todo s.o. fs.Write(BitConverter.GetBytes(ln));

            Span<byte> span = new(buffer);
            var fIn = file.FileInfo.OpenRead();

            long ptr = 0;
            while (ptr < file.Length)
            {
                var read = fIn.Read(span);

                fs.Write(read == BufferLength ? span : span.Slice(0, read));

                ptr += read;
            }
            fs.Flush();
        }

        private static void WriteString(string value, Stream fs)
        {
            if (value.Length >= ushort.MaxValue) throw new ArgumentException($"Max Path Length: {ushort.MaxValue}");
            var ln = (ushort)value.Length;

            fs.Write(BitConverter.GetBytes(ln));
            fs.Write(Encoding.UTF8.GetBytes(value));
        }

        private static IEnumerable<FileInfo> RecEnumerator(DirectoryInfo dir)
        {
            return dir.EnumerateFiles().Concat(dir.EnumerateDirectories().SelectMany(RecEnumerator));
        }

        public AjivaInstallInfo FromPack(string pack)
        {
            FileStream fs = File.OpenRead(File.Exists(pack) ? pack : File.Exists(pack + PackageExtension) ? pack + PackageExtension : throw new ArgumentException("Path is not a File!"));

            var head = ReadHead(fs);

            var info = new AjivaInstallInfo
            {
                Arguments = head.Arguments,
                Name = head.Name,
                Description = head.Description,
                Executabe = head.Executable
            };

            FileDescriptor[] files = new FileDescriptor[head.FilesCount];

            byte[] headBuffer = new byte[head.HeadLength];
            fs.Read(headBuffer);
            MemoryStream headBufferStr = new(headBuffer);

            for (var i = 0; i < head.FilesCount; i++)
            {
                files[i] = FileDescriptor.ReadHeadFrom(headBufferStr);
            }

            var ln = fs.Length - fs.Position;

            AjivaInstallInfo BuildFromMemory()
            {
                Memory<byte> fileBuffer = new byte[ln]; // all data except header
                fs.Read(fileBuffer.Span);

                foreach (var file in files)
                {
                    info.Files.Add(new MemoryInstallerFile {Data = fileBuffer.Slice((int)file.Pos, (int)file.Length), Length = file.Length, Location = file.RelPath});
                }

                return info;
            }

            AjivaInstallInfo BuildFromFile()
            {
                var pos = fs.Position;
                object syncLock = new();
                
                foreach (var file in files.OrderBy(x => x.Pos))
                {
                    info.Files.Add(new PackFileInstallerFile(fs, ref syncLock, pos + file.Pos){ Length = file.Length, Location = file.RelPath});
                }
                
                return info;
            }

            return ln < int.MaxValue ? BuildFromMemory() : BuildFromFile();
        }

        private readonly byte[] buffer = new byte[BufferLength];
        private readonly Memory<byte> memForUShort = new byte[sizeof(ushort)];
        private readonly Memory<byte> memForLong = new byte[sizeof(long)];

        private string ReadString(FileStream fs)
        {
            if (fs.Read(memForUShort.Span) < memForUShort.Length) throw new EndOfStreamException();

            var length = BitConverter.ToUInt16(memForUShort.Span);

            var str = new Span<byte>(buffer, 0, length);

            if (fs.Read(str) < str.Length) throw new EndOfStreamException();

            return Encoding.UTF8.GetString(str);
        }

        public static MemoryInstallerFile FromMemory(Memory<byte> data, out int dataLength)
        {
            var strLn = (int)BitConverter.ToUInt16(data.Span);

            var location = Encoding.UTF8.GetString(data.Span.Slice(sizeof(ushort), strLn));

            var length = BitConverter.ToInt64(data.Span.Slice(sizeof(ushort) + strLn));
            dataLength = sizeof(ushort) + sizeof(long) + strLn + (int)length;

            return new()
            {
                Length = length,
                Location = location,
                Data = data.Slice(sizeof(ushort) + sizeof(long) + strLn, (int)length)
            };
        }

        private PackFileInstallerFile FromFile(FileStream fs, long head, ref object syncLock, out long dataLength)
        {
            fs.Seek(head, SeekOrigin.Begin);
            var location = ReadString(fs);

            if (fs.Read(memForLong.Span) < memForLong.Length) throw new EndOfStreamException();

            var length = BitConverter.ToInt64(memForLong.Span);

            dataLength = sizeof(ushort) + sizeof(long) + location.Length + length;

            return new(fs, ref syncLock, head + sizeof(ushort) + sizeof(long) + location.Length)
            {
                Length = length,
                Location = location,
            };
        }

        private static T? ReadJsonObjectAs<T>(Stream pack, out uint length) where T : class, new()
        {
            Span<byte> tmp = new byte[sizeof(uint)];
            pack.Read(tmp);
            var jsonLength = BitConverter.ToUInt32(tmp);
            tmp = new byte[jsonLength];
            pack.Read(tmp);
            var json = Encoding.UTF8.GetString(tmp);
            length = sizeof(uint) + jsonLength;
            return JsonSerializer.Deserialize<T>(json);
        }

        private static uint WriteAsJson<T>(Stream pack, T head) where T : class, new()
        {
            var json = JsonSerializer.Serialize(head);
            var bits = Encoding.UTF8.GetBytes(json);
            pack.Write(BitConverter.GetBytes((uint)bits.Length));
            pack.Write(bits);
            return (uint)(bits.Length + sizeof(uint));
        }

        private static bool OpenFile(string @out, out FileStream fs)
        {
            if (File.Exists(@out))
            {
                if (!LogHelper.YesNow($"Delete Existing file: {@out}"))
                {
                    fs = default!;
                    return false;
                }

                File.Delete(@out);
            }
            fs = File.Open(@out, FileMode.CreateNew, FileAccess.Write)!;
            return true;
        }
    }
}
