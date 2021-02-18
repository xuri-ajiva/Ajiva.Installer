using System.IO;
using Ajiva.Installer.Core.Installer.FileTypes;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class StructureFile : IWritable
    {
        public StructureFile(StructureDirectory parent, FileInfo fileInfo, ref long position)
        {
            Parent = parent;
            Name = fileInfo.Name;
            Attributes = fileInfo.Attributes;
            Length = fileInfo.Length;
            Pos = position;
            position = Pos + Length;
        }

        /// <summary>
        /// only for read
        /// </summary>
        public StructureFile(StructureDirectory parent)
        {
            Parent = parent;
        }

        public StructureDirectory Parent { get; set; }
        public string Name { get; set; }
        public long Pos { get; set; }
        public long Length { get; set; }
        public FileAttributes Attributes { get; set; }
        
        public IInstallerFile? File { get; set; }

        public void WriteTo(Stream stream)
        {
            stream.WriteValue64(Pos);
            stream.WriteValue64(Length);
            stream.WriteValue32((int)Attributes);
            stream.WriteString(Name);
        }

        public void ReadFrom(Stream stream)
        {
            Pos = stream.ReadValue64();
            Length = stream.ReadValue64();
            Attributes = (FileAttributes)stream.ReadValue32();
            Name = stream.ReadString();
        }
    }
}