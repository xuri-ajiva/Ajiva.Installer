using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Ajiva.Installer.Core.Installer.Pack;

namespace Ajiva.Installer.Core.Installer
{
    internal class AjivaInstaller : IDisposable
    {
        public event Action<double>? PersentageChanged;
        public event Action<string>? InfoChanged;
        public Action<string> Logger;

        public long TotalBytes;
        public long DoneBytes;
        public long TotalFiles;
        public long DoneFiles;

        public void InstallAsync(AjivaInstallInfo installInfo, string path)
        {
            Logger("Installing:  " + installInfo.Name);
            Logger("Description: " + installInfo.Description);

            path = Path.GetFullPath(path);

            DirectoryCreation(path);

            long files = 0;
            long bytes = 0;

            var cache = BuildPathCache(path);

            void AddRecursive(string dirPathRec, StructureDirectory directory)
            {
                dirPathRec = Path.Combine(dirPathRec, directory.Name);
                Directory.CreateDirectory(dirPathRec);

                foreach (var infoFile in directory.Files)
                {
                    bytes += infoFile.Length;
                    files++;
                    ToInstall.Enqueue(new(path, dirPathRec, infoFile.File!));
                }
                foreach (var structureDirectory in directory.Directories)
                {
                    AddRecursive(dirPathRec, structureDirectory);
                }
            }

            AddRecursive("", installInfo.Root);

            sync.Release((int)files);
            Interlocked.Add(ref TotalBytes, bytes);
            Interlocked.Add(ref TotalFiles, files);
        }

        private readonly Thread[] workers;

        private readonly Semaphore sync = new(0, int.MaxValue);

        public ConcurrentQueue<InstallerData> ToInstall = new();

        public AjivaInstaller(int workersCount, Action<string> logger)
        {
            Logger = logger;
            workers = new Thread[workersCount];
            for (var i = 0; i < workersCount; i++)
            {
                workers[i] = new(InstallWork);
                workers[i].Start();
            }
        }

        public bool Disposed { get; private set; }

        private void InstallWork()
        {
            while (!Disposed)
            {
                sync.WaitOne();

                while (true)
                {
                    if (ToInstall.IsEmpty) break;

                    if (!ToInstall.TryDequeue(out var inst)) continue;

                    WorkFile(inst);
                    break;
                }

                if (!ToInstall.IsEmpty) continue;

                PresentPercent();
                //Logger("Finished!");
            }
        }

        private void WorkFile(InstallerData result)
        {
            const int saveInterval = 0x10000000; //250 mb
            var path = result.SavePath();

            if (File.Exists(path))
            {
                File.Delete(path);
                Logger($"Deleted: {path}");
            }

            DirectoryCreation(Path.GetDirectoryName(path)!);

            Logger($"Opening File: {path}");
            var fs = File.Open(path, FileMode.CreateNew, FileAccess.Write);
            fs.SetLength(result.InfoFile.Length);
            fs.Position = 0;

            var pos = 0;
            foreach (var data in result.InfoFile.GetData())
            {
                pos += data.Length;
                fs.Write(data.Span);

                if (pos % saveInterval == 0)
                {
                    PresentsChange();
                    fs.Flush();
                }
                Interlocked.Add(ref DoneBytes, data.Length);
            }

            fs.Flush();
            fs.Close();
            if (pos < saveInterval) PresentsChange();

            Interlocked.Increment(ref DoneFiles);
        }

        private void DirectoryCreation(string dir)
        {
            if (Directory.Exists(dir)) return;
            Directory.CreateDirectory(dir);
            Logger($"Creating Directory: {dir}");
        }

        private DateTime nextPresent = DateTime.MinValue;
        private readonly object nextPresentLock = new();

        private void PresentsChange()
        {
            lock (nextPresentLock)
            {
                if (DateTime.Now <= nextPresent)
                    return;

                nextPresent = DateTime.Now.AddMilliseconds(50);
            }

            PresentPercent();
        }

        private void PresentPercent()
        {
            var percentage = (double)DoneBytes / TotalBytes;
            PersentageChanged?.Invoke(percentage);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disposed = true;
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = null!;
            }
            sync.Dispose();
        }

        private static Dictionary<StructureSpecialFolder, string> BuildPathCache(string installationPath)
        {
            Dictionary<StructureSpecialFolder, string> result = new();

            var values = Enum.GetValues(typeof(StructureSpecialFolder)).Cast<StructureSpecialFolder>();

            foreach (var key in values)
            {
                string value;
                if (key >= StructureSpecialFolder.BeginCustomFolders)
                    switch (key)
                    {
                        case StructureSpecialFolder.InstallLocation:
                            value = installationPath;
                            break;
                        case StructureSpecialFolder.None:
                        case StructureSpecialFolder.BeginCustomFolders:
                            continue;
                        default:
                            throw new InvalidOperationException();
                    }
                else
                    value = Environment.GetFolderPath((Environment.SpecialFolder)(byte)key);

                result.Add(key, value);
            }

            return result;
        }

        private static string MakeStructurePath(StructureDirectory directory, IReadOnlyDictionary<StructureSpecialFolder, string> pathCache, string currentRec) =>
            Path.Combine(directory.ParentFolder == StructureSpecialFolder.None ? currentRec : pathCache[directory.ParentFolder], directory.Name);
    }
}
