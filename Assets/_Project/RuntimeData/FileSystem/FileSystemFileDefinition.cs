using System;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class FileSystemFileDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string name;

        [SerializeField]
        private FileSystemFileType fileType;

        [SerializeField]
        private long size;

        [SerializeField]
        private string modifiedDate;

        [SerializeField]
        private bool hidden;

        [SerializeField]
        private bool readOnly = true;

        [SerializeField]
        private bool starred;

        [Header("Content")]
        [SerializeField]
        [TextArea(4, 12)]
        private string textContent;

        [SerializeField]
        private Sprite previewImage;

        [Header("Integration")]
        [SerializeField]
        private string contentId;

        [SerializeField]
        private string progressionEventId;

        public string Id => id;
        public string Name => name;
        public FileSystemFileType FileType => fileType;
        public long Size => size;
        public string ModifiedDate => modifiedDate;
        public bool Hidden => hidden;
        public bool ReadOnly => readOnly;
        public bool Starred => starred;
        public string TextContent => textContent;
        public Sprite PreviewImage => previewImage;
        public string ContentId => contentId;
        public string ProgressionEventId => progressionEventId;
    }
}