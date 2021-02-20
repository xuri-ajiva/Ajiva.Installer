using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;

namespace Ajiva.Installer
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static int Main(string[] args)
        {
            Config.Load();

            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        private static readonly List<RunningInfo> Running = new();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void StartInstalled(InstalledInfo installedInfo)
        {
            Running.RemoveAll(x => !x.Running);
            if (Running.FirstOrDefault(x => x.Info == installedInfo) is { } first)
            {
                SetForegroundWindow(first.Proc.MainWindowHandle);

                return;
            }

            var run = new RunningInfo
            {
                Info = installedInfo,
                Proc = new()
                {
                    StartInfo = installedInfo.StartInfo(),
                }
            };

            Running.Add(run);

            run.Proc.Start();
        }

        public static void StartInstall(Uri url, string path)
        {
            if (!url.IsFile)
            {
                return;
            }
            InstalledInfo newItem = new()
            {
                Path = path,
                Progress = 0
            };

            Config.InstalledPrograms.Add(newItem);
            var info = Core.Program.Install(new() {PackPath = url.LocalPath, InstallPath = path}, false, d =>
            {
                newItem.Progress = d >= 1 ? 0 : d * 100;
            });

            newItem.Description = info.Info.Description;
            newItem.Name = info.Info.Name;
            newItem.ExecutingOptions.Args = info.Info.Arguments;
            newItem.ExecutingOptions.Executable = Path.Combine(nameof(newItem.Path).ToDynamic(), info.Root.Name, info.Info.Executable);
            newItem.ExecutingOptions.WorkDirectory = Path.GetDirectoryName(newItem.ExecutingOptions.Executable);

            Config.Save();
        }
    }
}
