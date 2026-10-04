using System;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class FileLocationDefinition
    {
        [SerializeField]
        private FileSystemLocation location;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private string folderId;

        [SerializeField]
        private bool visible = true;

        public FileSystemLocation Location => location;
        public string DisplayName => displayName;
        public string FolderId => folderId;
        public bool Visible => visible;
    }
}