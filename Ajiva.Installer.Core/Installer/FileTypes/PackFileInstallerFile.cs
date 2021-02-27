using System;
using System.Collections.Generic;
using System.IO;

namespace Ajiva.Installer.Core.Installer.FileTypes
{
    public class PackFileInstallerFile : IInstallerFile
    {
        private readonly Stream pack;
        private readonly object fileLock;
        private readonly long position;

        public PackFileInstallerFile(Stream pack, ref object fileLock, long position)
        {
            this.pack = pack;
            this.fileLock = fileLock;
            this.position = position;
        }

        /// <inheritdoc />
        public string Location { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        private const int ChunkSize = 0x1000000;

        /// <inheritdoc />
        public IEnumerable<Memory<byte>> GetData()
        {
            Memory<byte> buffer = new byte[ChunkSize];

            for (var i = 0;; i++)
            {
                if ((i + 1) * ChunkSize > Length)
                {
                    buffer = buffer.Slice(0, (int)(Length - i * ChunkSize));
                    lock (fileLock)
                    {
                        pack.Seek(position + i * ChunkSize, SeekOrigin.Begin);
                        pack.Read(buffer.Span);
                        yield return buffer;
                        yield break;
                    }
                }

                int read;
                lock (fileLock)
                {
                    pack.Seek(position + i * ChunkSize, SeekOrigin.Begin);
                    read = pack.Read(buffer.Span);
                }
                if (i * ChunkSize + read > Length)
                {
                    yield return buffer.Slice(0, (int)(Length - i * ChunkSize));
                    yield break;
                }

                yield return buffer;
            }
        }
    }
}
