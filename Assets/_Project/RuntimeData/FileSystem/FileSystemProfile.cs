using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class FileSystemProfile
    {
        [SerializeField]
        private string userId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private string homeFolderId;

        [SerializeField]
        private List<FileLocationDefinition> locations = new();

        [SerializeField]
        private List<FileSystemFolderDefinition> rootFolders = new();

        public string UserId => userId;
        public string DisplayName => displayName;
        public string HomeFolderId => homeFolderId;

        public IReadOnlyList<FileLocationDefinition> Locations =>
            locations;

        public IReadOnlyList<FileSystemFolderDefinition> RootFolders =>
            rootFolders;

        public FileLocationDefinition GetLocation(
            FileSystemLocation location)
        {
            foreach (FileLocationDefinition definition in locations)
            {
                if (definition == null)
                    continue;

                if (definition.Location == location)
                    return definition;
            }

            return null;
        }
    }
}