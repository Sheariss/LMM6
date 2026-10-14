using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class ImageContentEntry
    {
        [SerializeField] private string contentId;
        [SerializeField] private Sprite image;

        public string ContentId => contentId;
        public Sprite Image => image;
    }

    [CreateAssetMenu(
        fileName = "ImageContentCatalog",
        menuName = "ATLAS/SOS/Image Content Catalog")]
    public sealed class ImageContentCatalog : ScriptableObject
    {
        [SerializeField]
        private List<ImageContentEntry> entries = new();

        public bool TryResolve(
            string contentId,
            out Sprite image,
            out string error)
        {
            image = null;
            error = null;

            if (string.IsNullOrWhiteSpace(contentId))
            {
                error = "The image file has no content ID.";
                return false;
            }

            ImageContentEntry match = null;

            foreach (ImageContentEntry entry in entries)
            {
                if (entry == null ||
                    !string.Equals(
                        entry.ContentId,
                        contentId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    error =
                        $"Image content ID '{contentId}' is duplicated " +
                        "in the image catalog.";

                    return false;
                }

                match = entry;
            }

            if (match == null)
            {
                error =
                    $"Image content '{contentId}' was not found.";

                return false;
            }

            if (match.Image == null)
            {
                error =
                    $"Image content '{contentId}' has no assigned sprite.";

                return false;
            }

            image = match.Image;
            return true;
        }
    }
}