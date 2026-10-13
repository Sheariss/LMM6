using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [CreateAssetMenu(
        fileName = "FileSystemLibrary",
        menuName = "ATLAS/SOS/File System Library")]
    public sealed class FileSystemLibrary : ScriptableObject
    {
        [SerializeField]
        private Sprite defaultFolderIcon;

        [SerializeField]
        private List<FileSystemProfile> profiles = new();

        public Sprite DefaultFolderIcon => defaultFolderIcon;

        public IReadOnlyList<FileSystemProfile> Profiles => profiles;

        public FileSystemProfile GetProfile(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            foreach (FileSystemProfile profile in profiles)
            {
                if (profile == null)
                    continue;

                if (string.Equals(
                    profile.UserId,
                    userId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            return null;
        }

        public FileLocationDefinition GetLocationForFolder(
            string userId,
            string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return null;

            FileSystemProfile profile = GetProfile(userId);

            if (profile == null)
                return null;

            foreach (FileLocationDefinition location in profile.Locations)
            {
                if (location == null)
                    continue;

                if (string.Equals(
                    location.FolderId,
                    folderId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return location;
                }
            }

            return null;
        }

        public Sprite GetFolderIcon(string userId, string folderId)
        {
            FileLocationDefinition location =
                GetLocationForFolder(userId, folderId);

            return location?.Icon != null
                ? location.Icon
                : defaultFolderIcon;
        }
    }
}