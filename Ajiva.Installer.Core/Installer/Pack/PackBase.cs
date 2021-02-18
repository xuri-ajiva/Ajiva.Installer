namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class PackBase
    {
        protected const int BufferLength = 4194304;
        protected static readonly byte[] PackHeader = {(byte)'I', (byte)'N', (byte)'S', (byte)'T', (byte)'_', (byte)'P', (byte)'A', (byte)'C', (byte)'K'};
        protected readonly byte[] Buffer = new byte[BufferLength];
    }
}