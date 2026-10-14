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

        public FileSystemFileType FileType => fileType;
        public string AppId => appId;
    }

    [CreateAssetMenu(
        fileName = "FileAssociationCatalog",
        menuName = "ATLAS/SOS/File Associations")]
    public sealed class FileAssociationCatalog : ScriptableObject
    {
        [SerializeField]
        private List<FileAssociation> associations = new();

        public bool TryGetAppId(
            FileSystemFileType fileType,
            out string appId,
            out string error)
        {
            appId = null;
            error = null;

            FileAssociation match = null;

            foreach (FileAssociation association in associations)
            {
                if (association == null ||
                    association.FileType != fileType)
                {
                    continue;
                }

                if (match != null)
                {
                    error =
                        $"Multiple default apps are configured for " +
                        $"file type '{fileType}'.";

                    return false;
                }

                match = association;
            }

            if (match == null)
            {
                error =
                    $"No app is configured to open " +
                    $"file type '{fileType}'.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(match.AppId))
            {
                error =
                    $"The app association for '{fileType}' " +
                    $"has no app ID.";

                return false;
            }

            appId = match.AppId;
            return true;
        }
    }
}