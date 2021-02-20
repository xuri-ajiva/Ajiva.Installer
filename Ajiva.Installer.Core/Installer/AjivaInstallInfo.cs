using System.Collections.Generic;
using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;
using Ajiva.Installer.Core.Installer.Pack;

namespace Ajiva.Installer.Core.Installer
{
    public class AjivaInstallInfo
    {
        public InstallerInfo Info;

        public StructureDirectory Root { get; set; } = new();
    }
}
