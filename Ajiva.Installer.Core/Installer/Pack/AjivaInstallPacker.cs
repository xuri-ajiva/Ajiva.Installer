using System;
using System.IO;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer.Pack
{
    public class AjivaInstallPacker
    {
        private readonly Action<string> logger;
        private const string PackageExtension = ".inst_pack";

        public void BuildPack(string path, InstallerInfo info)
        {
            path = Path.GetFullPath(path);
            var @out = Path.Combine(path, "..", info.Name + PackageExtension);
            if (!OpenFile(@out, out FileStream pack)) return;

            var di = new DirectoryInfo(path);

            var root = new StructureDirectory(StructureSpecialFolder.InstallLocation, di.Name);

            long posRef = 0;
            BuildTreeRecursive(di, ref root, ref posRef);
            Array.Resize(ref root.Directories, root.Directories.Length + 1);
            root.Directories[^1] = new(root, "information");

            WritePack(path, pack, root, info, posRef);
            LogHelper.Log($"Finished: {@out}");
        }

        private void WritePack(string path, Stream stream, StructureDirectory root, InstallerInfo info, double length)
        {
            var pack = new PackBuilder();

            pack.BeginFileInfo(stream);

            pack.WriteHeader(info);

            pack.WriteStructure(root);

            var basePos = stream.Position;

            void WriteFilesRec(string dirPathRec, StructureDirectory directory)
            {
                dirPathRec = Path.Combine(dirPathRec, directory.Name);
                foreach (var file in directory.Files)
                {
                    var save = stream.Position;
                    try
                    {
                        pack.WriteFile(dirPathRec, file);
                        logger($"[{(stream.Position - basePos) / length:0.000000%}] File: {file.Name}");
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

        public AjivaInstallInfo FromPack(string path) => FromPack(path, logger);

        public static AjivaInstallInfo FromPack(string path, Action<string>? logger)
        {
            FileStream fs = File.OpenRead(File.Exists(path) ? path : File.Exists(path + PackageExtension) ? path + PackageExtension : throw new ArgumentException("Path is not a File!"));

            Stream data = new BufferedStream(fs);

            var pack = new PackReader(data);

            pack.ReadHead();

            pack.ReadStructure();

            logger?.Invoke($"Hash: {pack.Info.StructureHash}");

            var ln = data.Length - data.Position;

            logger?.Invoke($"Contend Length: {ln}");

            void BuildFromMemory(StructureDirectory root)
            {
                logger?.Invoke("Building FileStructure in Memory");
                Memory<byte> fileBuffer = new byte[ln]; // all data except header
                data.Read(fileBuffer.Span);

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

                RecFillFiles(root);
            }

            void BuildFromFile(StructureDirectory root)
            {
                logger?.Invoke("Building FileStructure with Reference to Pack");

                var pos = data.Position;
                object syncLock = new();

                void RecFillFiles(StructureDirectory dir)
                {
                    foreach (var file in dir.Files)
                    {
                        file.File = new PackFileInstallerFile(data, ref syncLock, pos + file.Pos) {Length = file.Length, Location = file.Name};
                    }
                    foreach (var directory in dir.Directories)
                    {
                        RecFillFiles(directory);
                    }
                }

                RecFillFiles(root);
            }

            if (ln < int.MaxValue)
                BuildFromMemory(pack.Info.Root);
            else BuildFromFile(pack.Info.Root);

            return pack.Info;
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
