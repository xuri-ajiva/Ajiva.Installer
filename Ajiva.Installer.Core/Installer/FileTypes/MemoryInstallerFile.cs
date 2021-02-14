using System;
using System.Collections.Generic;

namespace Ajiva.Installer.Core.Installer.FileTypes
{
    public class MemoryInstallerFile : IInstallerFile
    {
        internal Memory<byte> Data;

        /// <inheritdoc />
        public string Location { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public IEnumerable<Memory<byte>> GetData()
        {
            for (var i = 0;; i++)
            {
                if (Data.Length - i * ChunkSize < ChunkSize)
                {
                    yield return Data.Slice(i * ChunkSize);
                    yield break;
                }

                yield return Data.Slice(i * ChunkSize, ChunkSize);
            }
        }

        private const int ChunkSize = 0x1000000;
    }
}