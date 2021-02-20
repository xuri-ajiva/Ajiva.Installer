using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Ajiva.Installer.Core.Installer.Pack;

namespace Ajiva.Installer.Core.Installer
{
    public class AjivaInstaller : IDisposable
    {
        //public event Action<double>? PercentageChanged;
        public event Action<string>? InfoChanged;
        public readonly int WorkersCount;
        public Action<string> Logger;

        public long TotalBytes;
        public long DoneBytes;
        public long TotalFiles;
        public long DoneFiles;

        public void InstallAsync(AjivaInstallInfo installInfo, string path, Action<double> percentageChanged)
        {
            Logger("Installing:  " + installInfo.Info.Name);
            Logger("Description: " + installInfo.Info.Description);

            path = Path.GetFullPath(path);

            DirectoryCreation(path);

            long files = 0;
            long bytes = 0;

            var cache = BuildPathCache(path);

            void AddRecursive(string dirPathRec, StructureDirectory directory)
            {
                dirPathRec = MakeStructurePath(directory, cache, dirPathRec);
                if (!Directory.Exists(dirPathRec))
                {
                    Directory.CreateDirectory(dirPathRec);
                    Logger($"Creating Directory: {directory.Name}");
                }

                foreach (var infoFile in directory.Files)
                {
                    bytes += infoFile.Length;
                    files++;
                    ToInstall.Enqueue(new(percentageChanged, path, dirPathRec, infoFile.File!));
                }
                foreach (var structureDirectory in directory.Directories)
                {
                    AddRecursive(dirPathRec, structureDirectory);
                }
            }

            AddRecursive("", installInfo.Root);

            Interlocked.Add(ref TotalBytes, bytes);
            Interlocked.Add(ref TotalFiles, files);
            Finished = 0;
            sync.Release((int)files);
        }

        private readonly Thread[] workers;

        private readonly Semaphore sync = new(0, int.MaxValue);

        public ConcurrentQueue<InstallerData> ToInstall = new();

        public AjivaInstaller(int workersCount, Action<string> logger)
        {
            WorkersCount = workersCount;
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

                InstallerData? inst = null;
                while (true)
                {
                    if (ToInstall.IsEmpty) break;

                    if (!ToInstall.TryDequeue(out inst)) continue;

                    WorkFile(inst);
                    break;
                }

                if (!ToInstall.IsEmpty) continue;

                if (inst is not null) PresentPercent(inst.PercentageChanged);

                Interlocked.Increment(ref Finished);
            }
        }

        public int Finished;

        public bool IsFinished => Finished == WorkersCount;

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
                    PresentsChange(result.PercentageChanged);
                    fs.Flush();
                }
                Interlocked.Add(ref DoneBytes, data.Length);
            }

            fs.Flush();
            fs.Close();
            if (pos < saveInterval) PresentsChange(result.PercentageChanged);

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

        private void PresentsChange(Action<double> resultPercentageChanged)
        {
            lock (nextPresentLock)
            {
                if (DateTime.Now <= nextPresent)
                    return;

                nextPresent = DateTime.Now.AddMilliseconds(50);
            }

            PresentPercent(resultPercentageChanged);
        }

        private void PresentPercent(Action<double> resultPercentageChanged)
        {
            var percentage = (double)DoneBytes / TotalBytes;
            resultPercentageChanged?.Invoke(percentage);
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
