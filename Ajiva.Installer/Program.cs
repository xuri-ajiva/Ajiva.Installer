using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
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
            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

        private static List<RunningInfo> Running = new();

        public static void StartInstalled(InstalledInfo installedInfo)
        {
            Running.RemoveAll(x => !x.Running);
            if (Running.Any(x => x.Info == installedInfo)) return;

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
