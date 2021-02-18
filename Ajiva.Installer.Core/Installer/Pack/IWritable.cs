using System.IO;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal interface IWritable
    {
        void WriteTo(Stream stream);
        void ReadFrom(Stream stream);
    }
}