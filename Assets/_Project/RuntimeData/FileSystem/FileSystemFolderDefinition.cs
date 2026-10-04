using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class FileSystemFolderDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string name;

        [SerializeField]
        private bool hidden;

        [SerializeField]
        private bool readOnly = true;

        [SerializeField]
        private List<FileSystemFolderDefinition> folders = new();

        [SerializeField]
        private List<FileSystemFileDefinition> files = new();

        public string Id => id;
        public string Name => name;
        public bool Hidden => hidden;
        public bool ReadOnly => readOnly;

        public IReadOnlyList<FileSystemFolderDefinition> Folders =>
            folders;

        public IReadOnlyList<FileSystemFileDefinition> Files =>
            files;
    }
}