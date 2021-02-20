using System.Text.Json.Serialization;

namespace Ajiva.Installer.Core.Installer.Pack
{
    public record InstallerInfo(string Name, string Description, string Executable, string Arguments)
    {
        [JsonConstructor]
        public InstallerInfo() : this(default!, default!, default!, default!)
        {
        }
    }
}
