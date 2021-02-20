using System.Text.Json.Serialization;

namespace Ajiva.Installer.Core.Installer.Pack
{
    public record PackHead(long FilesCount, int HeadLength, InstallerInfo Information)
    {
        [JsonConstructor]
        public PackHead() : this(default!, default!, default!)
        {
        }
    }
}
