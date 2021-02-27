using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Ajiva.Installer.Core.Installer;
using Ajiva.Installer.Helpers;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace Ajiva.Installer
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static int Main(string[] args)
        {
            Interop.Console.Hide();
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

        public static void StartInstall(InstalledInfo installed, bool addToInstalled, Action<string?>? log = null, Action? finishCallback = null, bool useConsole = false)
        {
            if (addToInstalled)
            {
                Config.InstalledPrograms.Add(installed);
                Config.Save();
            }

            if (installed.Source is null) throw new ArgumentException("Source is null!", nameof(installed.Source), null);
            if (installed.Path is null) throw new ArgumentException("Path is null!", nameof(installed.Path), null);

            installed.AvailableAction = AvailableAction.Installing;

            if (!installed.Source.IsFile)
            {
                //TODO: download or something!
                return;
            }

            var isFinish = false;

            if (useConsole)
                Interop.Console.Show();

            var info = AjivaInstaller.InstallBlank(s =>
            {
                if (useConsole)
                    Console.WriteLine(s);

                if (log is null && !useConsole)
                    Debug.WriteLine(s);
                else log?.Invoke(s);
            }, new() {PackPath = installed.Source.LocalPath, InstallPath = installed.Path}, false, d =>
            {
                if (isFinish) return;
                if (d >= 1 && !isFinish)
                {
                    isFinish = true;
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        installed.AvailableAction = AvailableAction.Start;
                        Config.Save();
                        await Task.Delay(2000);
                        finishCallback?.Invoke();
                        if (useConsole)
                            Interop.Console.Hide();
                    });
                    installed.Progress = 0;
                }
                else
                    installed.Progress = d * 100;
            }, 64);

            installed.UniqueIdentifier = info.StructureHash;
            installed.Description ??= info.Info.Description;
            installed.Name ??= info.Info.Name;
            installed.IconSrc ??= Path.Combine(nameof(installed.Path).ToDynamic(), info.Root.Name, info.Info.IconSrc.TrimStart('\\'));
            installed.ExecutingOptions ??= new();
            installed.ExecutingOptions.Args ??= info.Info.Arguments;
            installed.ExecutingOptions.Executable ??= Path.Combine(nameof(installed.Path).ToDynamic(), info.Root.Name, info.Info.Executable.TrimStart('\\'));
            installed.ExecutingOptions.WorkDirectory ??= Path.GetDirectoryName(installed.ExecutingOptions.Executable)!;

            Config.Save();
        }
    }
}
