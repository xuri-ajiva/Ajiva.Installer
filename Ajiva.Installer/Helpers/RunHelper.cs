using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ajiva.Installer.ViewModels;

namespace Ajiva.Installer.Helpers
{
    public static class RunHelper
    {
        public static readonly List<RunningInfo> Running = new();

        public static async void TerminateRunning(InstalledInfo dc)
        {
            Running.RemoveAll(x => !x.Running);
            if (Running.FirstOrDefault(x => x.Info == dc) is not { } first) return;

            try
            {
                first.Proc.CloseMainWindow();
                await Task.Delay(100);
            }
            finally
            {
                first.Proc.Kill(true);
            }
        }

        public static void StartInstalled(InstalledInfo installedInfo)
        {
            Running.RemoveAll(x => !x.Running);
            if (Running.FirstOrDefault(x => x.Info == installedInfo) is { } first)
            {
                Interop.SetForegroundWindow(first.Proc.MainWindowHandle);

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

            Task.Factory.StartNew(async () =>
            {
                while (!run.Proc.HasExited) await Task.Delay(100);

                installedInfo.AvailableAction = AvailableAction.Start;
                Running.Remove(run);
            });
        }
    }
}
