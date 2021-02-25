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

        public static void StartInstall(InstalledInfo installed, bool addToInstalled)
        {
            if (addToInstalled)
                Config.InstalledPrograms.Add(installed);
            Config.Save();

            if (installed.Source is null) throw new ArgumentException("Source is null!", nameof(installed.Source), null);
            if (installed.Path is null) throw new ArgumentException("Path is null!", nameof(installed.Path), null);

            installed.AvailableAction = AvailableAction.Installing;

            if (!installed.Source.IsFile)
            {
                //TODO: download or something!
                return;
            }

            ShowWindow(GetConsoleWindow(), SW_SHOW);
            var info = AjivaInstaller.InstallBlank(Console.WriteLine, new() {PackPath = installed.Source.LocalPath, InstallPath = installed.Path}, false, d =>
            {
                if (d >= 1)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            installed.AvailableAction = AvailableAction.Start;
                            Config.Save();
                        });
                        await Task.Delay(2000);
                        ShowWindow(GetConsoleWindow(), SW_HIDE);
                    });
                    installed.Progress = 0;
                }
                else
                    installed.Progress = d * 100;
            }, 64);

            //todo add icon
            installed.Description ??= info.Info.Description;
            installed.Name ??= info.Info.Name;
            installed.ExecutingOptions ??= new();
            installed.ExecutingOptions.Args ??= info.Info.Arguments;
            installed.ExecutingOptions.Executable ??= Path.Combine(nameof(installed.Path).ToDynamic(), info.Root.Name, info.Info.Executable);
            installed.ExecutingOptions.WorkDirectory ??= Path.GetDirectoryName(installed.ExecutingOptions.Executable)!;
        }
    }
}
