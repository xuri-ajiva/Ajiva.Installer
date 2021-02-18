using System.Text.Json.Serialization;

namespace Ajiva.Installer.Core.Installer.Pack
{
    public record PackHead(long FilesCount, int HeadLength, string Name, string Description, string Executable, string Arguments)
    {
        [JsonConstructor]
        public PackHead() : this(default!, default!, default!, default!, default!, default!)
        {
        }
    }
}