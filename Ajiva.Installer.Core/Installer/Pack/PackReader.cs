using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class PackReader : PackBase
    {
        private Stream Data { get; }

        public AjivaInstallInfo Info { get; } = new();
        
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

            Info.Info = Data.ReadJsonObjectAs<InstallerInfo>(out _)!;
        }

        public void ReadStructure()
        {
            if (Info.Info is null) throw new InvalidOperationException();
            if (Info.Root is not null) throw new InvalidOperationException();
            Info.Root = new(StructureSpecialFolder.InstallLocation, "");

            var begin = Data.Position;

            Info.Root.ReadFrom(Data);

            var end = Data.Position;
            Debug.Assert(end >= begin, "end >= begin, Structure was less then 0 bytes long");

            byte[] structure = new byte[end - begin];

            Data.Position = begin;
            Data.Read(structure);

            Debug.Assert(Data.Position == end, "Data.Position == end");

            var hash = Md5CryptoServiceProvider.ComputeHash(structure);
            Info.StructureHash = new(hash);
        }

        private static readonly MD5CryptoServiceProvider Md5CryptoServiceProvider = new();
    }
}
