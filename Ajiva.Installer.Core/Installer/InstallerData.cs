using System;
using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    public record InstallerData(Action<double> PercentageChanged,string RootPath, string DirePath, IInstallerFile InfoFile)
    {
        public string SavePath()
        {
            return Path.Combine(RootPath, DirePath, InfoFile.Location);
        }
    }
}
