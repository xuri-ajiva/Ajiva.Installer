using System;
using System.IO;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class PackBuilder : PackBase
    {
        private readonly Stream structure;
        private Stream? data;

        public PackBuilder()
        {
            structure = new MemoryStream();
        }

        public void BuildStructure(StructureDirectory root)
        {
            if (structure.Position != 0)
                throw new ArgumentException("Already Written an structure");

            root.WriteTo(structure);
        }

        public void BeginType(Stream stream)
        {
            if (data != null) throw new InvalidOperationException();
            data = stream;
            stream.Write(PackHeader);
        }

        public long StructureLength => structure.Position;

        public void WriteHeader(PackHead packHead)
        {
            if (data == null) throw new InvalidOperationException();
            data.WriteAsJson(packHead);
        }

        public void WriteFile(string dirPath, StructureFile file)
        {
            if (data == null) throw new InvalidOperationException();

            Span<byte> span = new(Buffer);
            var fIn = File.OpenRead(Path.Join(dirPath, file.Name));

            long ptr = 0;
            while (ptr < file.Length)
            {
                var read = fIn.Read(span);

                data.Write(read == BufferLength ? span : span.Slice(0, read));

                ptr += read;
            }
            data.Flush();
        }

        public void WriteStructure()
        {
            structure.Position = 0; // write from beginning
            structure.CopyTo(data ?? throw new InvalidOperationException(), BufferLength);
        }
    }
}
