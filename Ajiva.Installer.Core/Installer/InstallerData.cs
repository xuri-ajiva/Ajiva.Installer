using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    internal record InstallerData(string RootPath, string DirePath, IInstallerFile InfoFile)
    {
        public string SavePath()
        {
            return Path.Combine(RootPath, DirePath, InfoFile.Location);
        }
    }
}
