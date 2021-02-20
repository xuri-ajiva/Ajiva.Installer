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
    }
}
