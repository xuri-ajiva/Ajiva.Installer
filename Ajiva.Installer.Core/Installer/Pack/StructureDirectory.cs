using System.IO;

namespace Ajiva.Installer.Core.Installer.Pack
{
    internal class StructureDirectory : IWritable
    {
        public string Name { get; set; }
        public StructureSpecialFolder ParentFolder { get; set; } //change to custom enum

        public StructureDirectory(StructureDirectory parent, string name)
        {
            Parent = parent;
            Name = name;
            ParentFolder = StructureSpecialFolder.None;
        }

        public StructureDirectory(StructureSpecialFolder parent, string name)
        {
            Parent = null;
            ParentFolder = parent;
            Name = name;
        }

        /// <summary>
        /// only for read
        /// </summary>
        public StructureDirectory()
        {
        }

        public StructureDirectory? Parent { get; }
        public StructureDirectory[] Directories { get; set; }
        public StructureFile[] Files { get; set; }

        /// <inheritdoc />
        public void WriteTo(Stream stream)
        {
            stream.WriteString(Name);
            stream.WriteByte((byte)ParentFolder);
            stream.WriteValue64(Files.LongLength);
            stream.WriteValue64(Directories.LongLength);
            foreach (var file in Files)
            {
                file.WriteTo(stream);
            }
            foreach (var directory in Directories)
            {
                directory.WriteTo(stream);
            }
        }

        /// <inheritdoc />
        public void ReadFrom(Stream stream)
        {
            Name = stream.ReadString();
            ParentFolder = (StructureSpecialFolder)stream.ReadByte();
            Files = new StructureFile[stream.ReadValue64()];
            Directories = new StructureDirectory[stream.ReadValue64()];
            for (var i = 0; i < Files.Length; i++)
            {
                Files[i] = new(this);
                Files[i].ReadFrom(stream);
            }
            for (var i = 0; i < Directories.Length; i++)
            {
                Directories[i] = new(this, "");
                Directories[i].ReadFrom(stream);
            }
        }
    }
}
