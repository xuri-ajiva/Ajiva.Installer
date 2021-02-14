using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    internal class AjivaInstallPacker
    {
        private static readonly byte[] Header = {(byte)'I', (byte)'N', (byte)'S', (byte)'T', (byte)'_', (byte)'P', (byte)'A', (byte)'C', (byte)'K'};
        private const string PackageExtension = ".inst_pack";

        private const int BufferLength = 4194304;

        public void Pack(string path, string name)
        {
            path = Path.GetFullPath(path);
            var @out = Path.Combine(path, "..", name + PackageExtension);
            if (File.Exists(@out))
            {
                if (!LogHelper.YesNow($"Delete Existing file: {@out}")) return;

                File.Delete(@out);
            }
            FileStream fs = File.Open(@out, FileMode.CreateNew, FileAccess.Write)!;
            List<FileInfo> files = new();

            RecEnumerator(new(path), files);
            fs.Write(Header);

            foreach (var file in files)
            {
                var pos = fs.Position;
                try
                {
                    WriteFile(file, path, fs);
                }
                catch (Exception e)
                {
                    LogHelper.Log($"Failed to add: {file.FullName} ({e.Message})");
                    fs.Position = pos;
                }
            }
            fs.Flush();
            fs.Close();
            LogHelper.Log($"Finished: {@out}");
        }

        private void WriteFile(FileInfo file, string path, Stream fs)
        {
            var fName = file.FullName.Substring(path.Length + 1);

            WriteString(fName, fs);
            var ln = file.Length;
            fs.Write(BitConverter.GetBytes(ln));

            Span<byte> span = new(buffer);
            var fIn = file.OpenRead();

            long ptr = 0;
            while (ptr < ln)
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

        private static void RecEnumerator(DirectoryInfo dir, List<FileInfo> files)
        {
            files.AddRange(dir.EnumerateFiles());

            foreach (var directory in dir.EnumerateDirectories()) RecEnumerator(directory, files);
        }

        public AjivaInstallInfo FromPack(string pack)
        {
            FileStream fs = File.OpenRead(File.Exists(pack) ? pack : File.Exists(pack + PackageExtension) ? pack + PackageExtension : throw new ArgumentException("Path is not a File!"));

            Span<byte> head = new byte[Header.Length];

            fs.Read(head);
            if (!head.SequenceEqual(Header))
            {
                fs.Close();
                throw new ArgumentException("File is not the right type!");
            }

            var info = new AjivaInstallInfo();
            info.Arguments = "TODO";
            info.Name = "TODO";
            info.Description = "TODO";
            info.Executabe = "TODO";

            var ln = fs.Length - fs.Position;

            AjivaInstallInfo BuildFromMemory()
            {
                var ptr = 0;
                byte[] fileBuffer = new byte[ln]; // all data except header

                fs.Read(fileBuffer.AsSpan());

                while (ptr < ln)
                {
                    info.Files.Add(FromMemory(fileBuffer.AsMemory(ptr), out var dataLength));
                    ptr += dataLength;
                }

                return info;
            }

            AjivaInstallInfo BuildFromFile()
            {
                var pos = fs.Position;
                object syncLock = new();

                while (pos < ln)
                {
                    info.Files.Add(FromFile(fs, pos, ref syncLock, out var dataLength));
                    pos += dataLength;
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
    }
}