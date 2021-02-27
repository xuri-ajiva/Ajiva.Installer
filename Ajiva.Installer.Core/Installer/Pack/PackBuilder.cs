using System;
using System.IO;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class PackBuilder : PackBase
    {
        private Stream? data;
        public void BeginFileInfo(Stream stream)
        {
            if (data != null) throw new InvalidOperationException();
            data = stream;
            stream.Write(PackHeader);
        }

        public void WriteHeader(InstallerInfo packHead)
        {
            if (data is null) throw new InvalidOperationException();
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

        public void WriteStructure(StructureDirectory root)
        {
            if (data is null) throw new InvalidOperationException();
            root.WriteTo(data);
        }
    }
}
