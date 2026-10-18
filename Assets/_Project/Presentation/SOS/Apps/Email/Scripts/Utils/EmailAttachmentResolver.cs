using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Presentation.SOS.FileExplorer;
using Atlas.SOS.Authentication;
using Atlas.SOS.FileSystem;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailResolvedAttachment
    {
        public string FileId { get; }
        public string Name { get; }
        public string Error { get; }
        public FileOpenRequest Request { get; }

        public bool IsAvailable =>
            Request != null;

        public long Size =>
            Request?.File?.Size ?? 0;

        public EmailResolvedAttachment(
            string fileId,
            FileOpenRequest request,
            string error = null)
        {
            FileId = fileId ?? string.Empty;
            Request = request;
            Error = error;

            Name = request?.File?.Name ??
                (string.IsNullOrWhiteSpace(fileId)
                    ? "Unavailable attachment"
                    : fileId);
        }
    }

    public sealed class EmailAttachmentResolver
    {
        private readonly string userId;
        private readonly string setupError;

        private readonly Dictionary<string, FileOpenRequest> files =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> ambiguousIds =
            new(StringComparer.Ordinal);

        public EmailAttachmentResolver(
            FileSystemLibrary library,
            string userId)
        {
            this.userId = userId;

            if (library == null)
            {
                setupError =
                    "No filesystem library is configured.";

                return;
            }

            FileSystemProfile profile =
                library.GetProfile(userId);

            if (profile == null)
            {
                setupError =
                    $"No filesystem profile exists for '{userId}'.";

                return;
            }

            FileSystemRuntimeBuilder builder = new();

            Dictionary<string, FileExplorerDirectory> directories =
                builder.Build(profile);

            foreach (FileExplorerDirectory directory
                     in directories.Values)
            {
                List<FileSystemFileDefinition> related =
                    directory.Items
                        .Where(item =>
                            item != null &&
                            !item.IsFolder &&
                            item.FileDefinition != null)
                        .Select(item => item.FileDefinition)
                        .ToList();

                foreach (FileExplorerItem item in directory.Items)
                {
                    if (item == null ||
                        item.IsFolder ||
                        item.FileDefinition == null ||
                        item.FileDefinition.Hidden ||
                        string.IsNullOrWhiteSpace(
                            item.FileDefinition.Id))
                    {
                        continue;
                    }

                    string id = item.FileDefinition.Id;

                    if (files.ContainsKey(id))
                    {
                        ambiguousIds.Add(id);
                        continue;
                    }

                    FileOpenRequest request = new(
                        userId,
                        item.Path,
                        item.FileDefinition,
                        related);

                    files.Add(id, request);
                }
            }
        }

        public EmailResolvedAttachment Resolve(string fileId)
        {
            string currentId =
                AuthenticationManager.Instance?
                    .CurrentUser?
                    .UserId;

            if (!string.Equals(
                    currentId,
                    userId,
                    StringComparison.Ordinal))
            {
                return Unavailable(
                    fileId,
                    "This attachment is unavailable to the current user.");
            }

            if (!string.IsNullOrEmpty(setupError))
                return Unavailable(fileId, setupError);

            if (string.IsNullOrWhiteSpace(fileId))
            {
                return Unavailable(
                    fileId,
                    "Attachment file ID is missing.");
            }

            if (ambiguousIds.Contains(fileId))
            {
                return Unavailable(
                    fileId,
                    $"Multiple files use attachment ID '{fileId}'.");
            }

            if (!files.TryGetValue(
                    fileId,
                    out FileOpenRequest request))
            {
                return Unavailable(
                    fileId,
                    $"Attached file '{fileId}' could not be found.");
            }

            if (request.File.Hidden)
            {
                return Unavailable(
                    fileId,
                    "The attached file is unavailable.");
            }

            return new EmailResolvedAttachment(
                fileId,
                request);
        }

        private static EmailResolvedAttachment Unavailable(
            string fileId,
            string error)
        {
            return new EmailResolvedAttachment(
                fileId,
                null,
                error);
        }
    }
}