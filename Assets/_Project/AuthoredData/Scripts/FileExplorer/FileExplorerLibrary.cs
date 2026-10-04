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
        private List<FileSystemProfile> profiles = new();

        public IReadOnlyList<FileSystemProfile> Profiles =>
            profiles;

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
    }
}