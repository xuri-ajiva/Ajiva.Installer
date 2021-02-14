using System;
using System.Collections.Generic;
using System.IO;

namespace Ajiva.Installer.Core.Installer.FileTypes
{
    internal class PhysicallyInstallerFile : IInstallerFile
    {
        private const int ChunkSize = 0x1000000;
        private readonly FileStream fs;

        /// <inheritdoc />
        public string Location { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public IEnumerable<Memory<byte>> GetData()
        {
            var buffer = new byte[ChunkSize];
            var mem = new Memory<byte>(buffer);

            while (fs.Position < Length)
            {
                var read = fs.Read(mem.Span);
                yield return mem.Slice(0, read);
            }
        }

        ~PhysicallyInstallerFile()
        {
            fs.Close();
        }

        public PhysicallyInstallerFile(string root, string path)
        {
            Location = path.Substring(root.Length + 1);

            fs = File.OpenRead(path);

            Length = fs.Length;
        }
    }
}