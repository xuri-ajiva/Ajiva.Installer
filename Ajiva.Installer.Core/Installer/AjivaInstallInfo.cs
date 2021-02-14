using System.Collections.Generic;
using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer
{
    internal class AjivaInstallInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Executabe { get; set; }
        public string Arguments { get; set; }

        public List<IInstallerFile> Files { get; set; } = new();

        public static AjivaInstallInfo DirCopy(string from)
        {
            from = Path.GetFullPath(from);

            var info = new AjivaInstallInfo
            {
                Name = "Copy",
                Description = $"Form {from}",
                Arguments = "",
                Executabe = "",
            };

            RecEnumerator(new(from));

            void RecEnumerator(DirectoryInfo dir)
            {
                foreach (var file in dir.EnumerateFiles())
                {
                    info!.Files.Add(new PhysicallyInstallerFile(from, file.FullName));
                }

                foreach (var directory in dir.EnumerateDirectories())
                {
                    RecEnumerator(directory);
                }
            }

            return info;
        }
    }
}
