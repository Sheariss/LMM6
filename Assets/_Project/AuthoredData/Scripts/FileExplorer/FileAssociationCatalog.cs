using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class FileAssociation
    {
        [SerializeField] private FileSystemFileType fileType;
        [SerializeField] private string appId;
        [SerializeField] private Sprite icon;

        [Tooltip(
            "Supported filename extensions, including the dot. " +
            "For example: .png, .jpg, .jpeg")]
        [SerializeField]
        private List<string> extensions = new();

        public FileSystemFileType FileType => fileType;
        public string AppId => appId;
        public Sprite Icon => icon;
        public IReadOnlyList<string> Extensions => extensions;

        internal bool MatchesExtension(string extension)
        {
            if (extensions == null)
                return false;

            foreach (string configured in extensions)
            {
                if (string.IsNullOrWhiteSpace(configured))
                    continue;

                string normalized = configured.Trim();

                if (!normalized.StartsWith(".", StringComparison.Ordinal))
                    normalized = "." + normalized;

                if (string.Equals(
                        normalized,
                        extension,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [CreateAssetMenu(
        fileName = "FileAssociationCatalog",
        menuName = "ATLAS/SOS/File Associations")]
    public sealed class FileAssociationCatalog : ScriptableObject
    {
        [SerializeField] private Sprite fallbackIcon;

        [SerializeField]
        private List<FileAssociation> associations = new();

        public IReadOnlyList<FileAssociation> Associations => associations;

        private bool TryGetAssociation(
            FileSystemFileType fileType,
            out FileAssociation result,
            out string error)
        {
            result = null;
            error = null;

            if (associations == null)
            {
                error = "The file association list is missing.";
                return false;
            }

            foreach (FileAssociation association in associations)
            {
                if (association == null ||
                    association.FileType != fileType)
                {
                    continue;
                }

                if (result != null)
                {
                    result = null;
                    error =
                        $"Multiple entries are configured for '{fileType}'.";

                    return false;
                }

                result = association;
            }

            if (result == null)
            {
                error =
                    $"No file association is configured for '{fileType}'.";

                return false;
            }

            return true;
        }

        public bool TryGetAppId(
            FileSystemFileType fileType,
            out string appId,
            out string error)
        {
            appId = null;

            if (!TryGetAssociation(fileType, out var association, out error))
                return false;

            if (string.IsNullOrWhiteSpace(association.AppId))
            {
                error =
                    $"The app association for '{fileType}' has no app ID.";

                return false;
            }

            appId = association.AppId;
            return true;
        }

        public Sprite GetIcon(FileSystemFileType fileType)
        {
            if (TryGetAssociation(fileType, out var association, out _) &&
                association.Icon != null)
            {
                return association.Icon;
            }

            return fallbackIcon;
        }

        public bool TryGetFileType(
            string fileName,
            out FileSystemFileType fileType)
        {
            return TryGetFileType(fileName, out fileType, out _);
        }

        public bool TryGetFileType(
            string fileName,
            out FileSystemFileType fileType,
            out string error)
        {
            fileType = FileSystemFileType.Unknown;
            error = null;

            string extension = GetExtension(fileName);

            if (string.IsNullOrEmpty(extension))
            {
                error = "The filename has no extension.";
                return false;
            }

            FileAssociation match = null;

            if (associations != null)
            {
                foreach (FileAssociation association in associations)
                {
                    if (association == null ||
                        !association.MatchesExtension(extension))
                    {
                        continue;
                    }

                    if (match != null)
                    {
                        error =
                            $"Extension '{extension}' appears in " +
                            "multiple file association entries.";

                        return false;
                    }

                    match = association;
                }
            }

            if (match == null)
            {
                error =
                    $"No file type is configured for '{extension}'.";

                return false;
            }

            fileType = match.FileType;
            return true;
        }

        private static string GetExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            string value = fileName.Trim();

            int separator = Math.Max(
                value.LastIndexOf('/'),
                value.LastIndexOf('\\'));

            int dot = value.LastIndexOf('.');

            if (dot <= separator || dot == value.Length - 1)
                return string.Empty;

            return value.Substring(dot);
        }
    }
}