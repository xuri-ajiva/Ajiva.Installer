using System;
using System.Collections.Generic;

namespace Ajiva.Installer.Core.Installer.FileTypes
{
    public interface IInstallerFile
    {
        public string Location { get; init; }

        public long Length { get; init; }

        public IEnumerable<Memory<byte>> GetData();
    }
}
