using System.Collections.Generic;

namespace Atlas.SOS.FileSystem
{
    public sealed class FileOpenRequest
    {
        public string UserId { get; }
        public string FilePath { get; }
        public FileSystemFileDefinition File { get; }

        public string ContentId => File?.ContentId;

        // Same-type files from the source folder, in display order.
        public IReadOnlyList<FileSystemFileDefinition> RelatedFiles { get; }

        public FileOpenRequest(
            string userId,
            string filePath,
            FileSystemFileDefinition file,
            IEnumerable<FileSystemFileDefinition> relatedFiles = null)
        {
            UserId = userId;
            FilePath = filePath;
            File = file;

            List<FileSystemFileDefinition> files = new();
            HashSet<string> seenIds = new(
                System.StringComparer.Ordinal);

            if (file != null && relatedFiles != null)
            {
                foreach (FileSystemFileDefinition related in relatedFiles)
                {
                    if (related == null ||
                        related.Hidden ||
                        related.FileType != file.FileType ||
                        string.IsNullOrWhiteSpace(related.Id))
                    {
                        continue;
                    }

                    // Always retain the actual selected definition.
                    FileSystemFileDefinition candidate =
                        related.Id == file.Id ? file : related;

                    if (seenIds.Add(candidate.Id))
                        files.Add(candidate);
                }
            }

            if (file != null && seenIds.Add(file.Id ?? string.Empty))
                files.Insert(0, file);

            RelatedFiles = files.AsReadOnly();
        }
    }
}