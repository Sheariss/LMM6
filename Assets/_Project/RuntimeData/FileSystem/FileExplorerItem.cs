using Atlas.SOS.FileSystem;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerItem
    {
        public string Id { get; }
        public string Name { get; }
        public string Path { get; }
        public FileExplorerItemType Type { get; }
        public FileSystemFileType FileType { get; }
        public long Size { get; }
        public string ModifiedDate { get; }
        public bool IsStarred { get; }
        public bool IsReadOnly { get; }
        public FileSystemFileDefinition FileDefinition { get; }

        public bool IsFolder =>
            Type == FileExplorerItemType.Folder;

        public FileExplorerItem(
            string id,
            string name,
            string path,
            FileExplorerItemType type,
            FileSystemFileType fileType = FileSystemFileType.Unknown,
            long size = 0,
            string modifiedDate = "",
            bool isStarred = false,
            bool isReadOnly = true,
            FileSystemFileDefinition fileDefinition = null)
        {
            Id = id;
            Name = name;
            Path = path;
            Type = type;
            FileType = fileType;
            Size = size;
            ModifiedDate = modifiedDate;
            IsStarred = isStarred;
            IsReadOnly = isReadOnly;
            FileDefinition = fileDefinition;
        }
    }
}