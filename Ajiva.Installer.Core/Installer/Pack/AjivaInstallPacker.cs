using System;
using System.IO;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class AjivaInstallPacker
    {
        private readonly Action<string> logger;
        private const string PackageExtension = ".inst_pack";

        public void Pack(string path, string name)
        {
            path = Path.GetFullPath(path);
            var @out = Path.Combine(path, "..", name + PackageExtension);
            if (!OpenFile(@out, out FileStream pack)) return;

            var di = new DirectoryInfo(path);

            var strDir = new StructureDirectory(StructureSpecialFolder.InstallLocation, di.Name);

            long posRef = 0;
            BuildTreeRecursive(di, ref strDir, ref posRef);

            WritePack(path, pack, strDir);
            LogHelper.Log($"Finished: {@out}");
        }

        private void WritePack(string path, Stream stream, StructureDirectory root)
        {
            var pack = new PackBuilder();

            pack.BuildStructure(root);

            pack.BeginType(stream);

            pack.WriteHeader(new(0, (int)pack.StructureLength, "aaa", "vvv", "bbb", "ddd"));

            pack.WriteStructure();

            void WriteFilesRec(string dirPathRec, StructureDirectory directory)
            {
                dirPathRec = Path.Combine(dirPathRec, directory.Name);
                foreach (var file in directory.Files)
                {
                    var save = stream.Position;
                    try
                    {
                        pack.WriteFile(dirPathRec, file);
                    }
                    catch (Exception e)
                    {
                        LogHelper.Log($"Failed to add: {file.Name} ({e.Message})");
                        file.Length = 0;
                        stream.Position = save; //reset head
                        stream.SetLength(save); //remove allocation
                        //todo set some error bit?
                    }
                }
                foreach (var structureDirectory in directory.Directories)
                {
                    WriteFilesRec(dirPathRec, structureDirectory);
                }
            }

            WriteFilesRec(Path.GetFullPath(path + "/../"), root);

            stream.Flush();
            stream.Close();
        }

        private void BuildTreeRecursive(DirectoryInfo dir, ref StructureDirectory root, ref long position)
        {
            logger($"Adding Directory: {dir.Name}");
            var files = dir.GetFiles();
            var dirs = dir.GetDirectories();
            root.Files = new StructureFile[files.Length];
            root.Directories = new StructureDirectory[dirs.Length];

            for (var i = 0; i < files.Length; i++)
            {
                root.Files[i] = new(root, files[i], ref position);

                logger($"Adding File: {root.Files[i].Name}");
            }
            for (var i = 0; i < dirs.Length; i++)
            {
                root.Directories[i] = new(root, dirs[i].Name);
                BuildTreeRecursive(dirs[i], ref root.Directories[i], ref position);
            }
        }

        public AjivaInstallInfo FromPack(string path)
        {
            FileStream fs = File.OpenRead(File.Exists(path) ? path : File.Exists(path + PackageExtension) ? path + PackageExtension : throw new ArgumentException("Path is not a File!"));

            var pack = new PackReader(fs);

            pack.ReadHead();
            var head = pack.Header!;

            var info = new AjivaInstallInfo
            {
                Arguments = head.Arguments,
                Name = head.Name,
                Description = head.Description,
                Executabe = head.Executable,
            };

            pack.ReadStructure();
            info.Root = pack.Root!;

            var ln = fs.Length - fs.Position;

            AjivaInstallInfo BuildFromMemory()
            {
                Memory<byte> fileBuffer = new byte[ln]; // all data except header
                fs.Read(fileBuffer.Span);

                void RecFillFiles(StructureDirectory dir)
                {
                    foreach (var file in dir.Files)
                    {
                        file.File = new MemoryInstallerFile {Data = fileBuffer.Slice((int)file.Pos, (int)file.Length), Length = file.Length, Location = file.Name};
                    }
                    foreach (var directory in dir.Directories)
                    {
                        RecFillFiles(directory);
                    }
                }

                RecFillFiles(pack.Root!);

                return info;
            }

            AjivaInstallInfo BuildFromFile()
            {
                var pos = fs.Position;
                object syncLock = new();

                void RecFillFiles(StructureDirectory dir)
                {
                    foreach (var file in dir.Files)
                    {
                        file.File = new PackFileInstallerFile(fs, ref syncLock, pos + file.Pos) {Length = file.Length, Location = file.Name};
                    }
                    foreach (var directory in dir.Directories)
                    {
                        RecFillFiles(directory);
                    }
                }

                RecFillFiles(pack.Root!);

                return info;
            }

            return ln < int.MaxValue ? BuildFromMemory() : BuildFromFile();
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

        public AjivaInstallPacker(Action<string> logger)
        {
            this.logger = logger;
        }
    }
}
