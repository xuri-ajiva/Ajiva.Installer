using System;
using System.IO;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class PackReader : PackBase
    {
        private Stream Data { get; }

        public PackHead? Header { get; private set; }

        public StructureDirectory? Root { get; private set; }

        public PackReader(Stream data)
        {
            Data = data;
        }

        public void ReadHead()
        {
            Span<byte> tmp = new byte[PackHeader.Length];

            Data.Read(tmp);
            if (!tmp.SequenceEqual(PackHeader))
            {
                Data.Close();
                throw new ArgumentException("File is not the right type!");
            }

            Header = Data.ReadJsonObjectAs<PackHead>(out _);
        }

        public void ReadStructure()
        {
            if (Header == null) throw new InvalidOperationException();
            if (Root != null) throw new InvalidOperationException();
            Root = new(StructureSpecialFolder.InstallLocation, Header.Name);
            Root.ReadFrom(Data);
        }
    }
}
