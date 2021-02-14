using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    internal record InstallerData(string RootPath, IInstallerFile InfoFile)
    {
        public string SavePath()
        {
            return SavePath(RootPath, InfoFile.Location);
        }

        private static string SavePath(string installInfoPath, string infoFileLocation)
        {
            return Path.Combine(installInfoPath, infoFileLocation);
        }
    }
}