using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AjivaNetHead
    {
        public int DataLength;
        public int Version;
        public int Index;
        public int ClientId;
        public int Type;

        public T GetType<T>() => (T)(object)Type;
    }
}
