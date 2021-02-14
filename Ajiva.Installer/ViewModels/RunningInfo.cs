using System.Diagnostics;

namespace Ajiva.Installer.ViewModels
{
    public class RunningInfo
    {
        public InstalledInfo Info { get; set; }

        public Process Proc { get; set; }

        public bool Running => !(Proc?.HasExited) ?? false;
    }
}