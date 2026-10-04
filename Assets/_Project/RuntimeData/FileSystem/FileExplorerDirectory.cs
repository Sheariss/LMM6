using System.Collections.Generic;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerDirectory
    {
        public string Id { get; }
        public string Path { get; }
        public string Name { get; }
        public string ParentPath { get; }
        public bool IsReadOnly { get; }
        public List<FileExplorerItem> Items { get; }

        public FileExplorerDirectory(
            string id,
            string path,
            string name,
            string parentPath,
            bool isReadOnly,
            List<FileExplorerItem> items)
        {
            Id = id;
            Path = path;
            Name = name;
            ParentPath = parentPath;
            IsReadOnly = isReadOnly;
            Items = items ?? new List<FileExplorerItem>();
        }
    }
}