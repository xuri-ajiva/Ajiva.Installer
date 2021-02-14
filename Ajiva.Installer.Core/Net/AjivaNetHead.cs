using System.Runtime.InteropServices;

namespace Ajiva.Installer.Core.Net
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AjivaNetHead
    {
        public int DataLength;
        public int Version;
        public int Index;
        public PaketType Type;
    }
}