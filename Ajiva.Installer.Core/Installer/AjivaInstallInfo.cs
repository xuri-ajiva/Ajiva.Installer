using System.Collections.Generic;
using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;
using Ajiva.Installer.Core.Installer.Pack;

namespace Ajiva.Installer.Core.Installer
{
    internal class AjivaInstallInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Executabe { get; set; }
        public string Arguments { get; set; }

        public StructureDirectory Root { get; set; } = new();
    }
}
