using System;
using System.Collections.Generic;
using Atlas.SOS.FileSystem;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileSystemRuntimeBuilder
    {
        private readonly Dictionary<string, FileExplorerDirectory> directories =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, FileExplorerDirectory> Build(
            FileSystemProfile profile)
        {
            directories.Clear();

            if (profile == null)
            {
                return new Dictionary<string, FileExplorerDirectory>(
                    StringComparer.OrdinalIgnoreCase);
            }

            foreach (FileSystemFolderDefinition folder in profile.RootFolders)
            {
                if (folder == null ||
                    folder.Hidden)
                {
                    continue;
                }

                BuildFolder(
                    folder,
                    "/",
                    "");
            }

            return new Dictionary<string, FileExplorerDirectory>(
                directories,
                StringComparer.OrdinalIgnoreCase);
        }

        private void BuildFolder(
            FileSystemFolderDefinition definition,
            string parentPath,
            string parentDirectoryPath)
        {
            string path =
                BuildPath(
                    parentPath,
                    definition.Name);

            List<FileExplorerItem> items =
                new();

            foreach (FileSystemFolderDefinition childFolder
                     in definition.Folders)
            {
                if (childFolder == null ||
                    childFolder.Hidden)
                {
                    continue;
                }

                string childPath =
                    BuildPath(
                        path,
                        childFolder.Name);

                items.Add(
                    new FileExplorerItem(
                        childFolder.Id,
                        childFolder.Name,
                        childPath,
                        FileExplorerItemType.Folder,
                        isReadOnly: childFolder.ReadOnly));
            }

            foreach (FileSystemFileDefinition file
                     in definition.Files)
            {
                if (file == null ||
                    file.Hidden)
                {
                    continue;
                }

                string filePath =
                    BuildPath(
                        path,
                        file.Name);

                items.Add(
                    new FileExplorerItem(
                        file.Id,
                        file.Name,
                        filePath,
                        FileExplorerItemType.File,
                        file.FileType,
                        file.Size,
                        file.ModifiedDate,
                        file.Starred,
                        file.ReadOnly,
                        file));
            }

            FileExplorerDirectory directory =
                new(
                    definition.Id,
                    path,
                    definition.Name,
                    parentDirectoryPath,
                    definition.ReadOnly,
                    items);

            directories[path] =
                directory;

            foreach (FileSystemFolderDefinition childFolder
                     in definition.Folders)
            {
                if (childFolder == null ||
                    childFolder.Hidden)
                {
                    continue;
                }

                BuildFolder(
                    childFolder,
                    path,
                    path);
            }
        }

        private static string BuildPath(
            string parent,
            string name)
        {
            string segment =
                NormalizeSegment(name);

            if (string.IsNullOrWhiteSpace(parent) ||
                parent == "/")
            {
                return "/" + segment;
            }

            return
                parent.TrimEnd('/') +
                "/" +
                segment;
        }

        private static string NormalizeSegment(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            return value
                .Trim()
                .Replace(" ", "-")
                .ToLowerInvariant();
        }
    }
}