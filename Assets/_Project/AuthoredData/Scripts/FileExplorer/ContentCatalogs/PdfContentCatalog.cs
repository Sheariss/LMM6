using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class PdfPageContent
    {
        [SerializeField] private Sprite image;
        [SerializeField] private Sprite thumbnail;

        [Tooltip("Page width at 100% zoom, in UI units. " +
                 "Height is calculated from the image aspect ratio.")]
        [Min(1f)]
        [SerializeField] private float displayWidth = 800f;

        [Tooltip("Text extracted from this page. Used for search.")]
        [TextArea(4, 16)]
        [SerializeField] private string searchableText;

        public Sprite Image => image;

        public Sprite Thumbnail =>
            thumbnail != null ? thumbnail : image;

        public float DisplayWidth => displayWidth;

        public string SearchableText =>
            searchableText ?? string.Empty;

        public Vector2 DisplaySize
        {
            get
            {
                if (image == null || image.rect.width <= 0f)
                    return Vector2.zero;

                return new Vector2(
                    displayWidth,
                    displayWidth * image.rect.height / image.rect.width);
            }
        }
    }

    [Serializable]
    public sealed class PdfOutlineContent
    {
        [SerializeField] private string title;

        [Tooltip("Target page number. The first page is 1.")]
        [Min(1)]
        [SerializeField] private int pageNumber = 1;

        [Tooltip("Hierarchy depth: 0 for a root entry, 1 for its child.")]
        [Min(0)]
        [SerializeField] private int level;

        public string Title => title;
        public int PageNumber => pageNumber;
        public int Level => level;
    }

    [Serializable]
    public sealed class PdfContentEntry
    {
        [SerializeField] private string contentId;
        [SerializeField] private string title;

        [Tooltip("Pages in document order.")]
        [SerializeField]
        private List<PdfPageContent> pages = new();

        [Tooltip("Outline entries in reading order. " +
                 "Children follow their parent.")]
        [SerializeField]
        private List<PdfOutlineContent> outline = new();

        public string ContentId => contentId;
        public string Title => title;

        public IReadOnlyList<PdfPageContent> Pages => pages;
        public IReadOnlyList<PdfOutlineContent> Outline => outline;

        public bool TryValidate(out string error)
        {
            error = null;

            if (pages == null || pages.Count == 0)
            {
                error = $"PDF content '{contentId}' has no pages.";
                return false;
            }

            for (int index = 0; index < pages.Count; index++)
            {
                PdfPageContent page = pages[index];

                if (page == null || page.Image == null)
                {
                    error =
                        $"PDF content '{contentId}', page {index + 1}, " +
                        "has no assigned image.";

                    return false;
                }

                if (page.Image.rect.width <= 0f ||
                    page.Image.rect.height <= 0f ||
                    page.DisplayWidth <= 0f ||
                    float.IsNaN(page.DisplayWidth) ||
                    float.IsInfinity(page.DisplayWidth))
                {
                    error =
                        $"PDF content '{contentId}', page {index + 1}, " +
                        "has invalid dimensions.";

                    return false;
                }
            }

            int previousLevel = -1;

            if (outline != null)
            {
                for (int index = 0; index < outline.Count; index++)
                {
                    PdfOutlineContent item = outline[index];

                    if (item == null ||
                        string.IsNullOrWhiteSpace(item.Title) ||
                        item.PageNumber < 1 ||
                        item.PageNumber > pages.Count ||
                        item.Level < 0 ||
                        item.Level > previousLevel + 1)
                    {
                        error =
                            $"PDF content '{contentId}' has an invalid " +
                            $"outline entry at position {index + 1}. " +
                            "Check its title, page number, and hierarchy.";

                        return false;
                    }

                    previousLevel = item.Level;
                }
            }

            return true;
        }
    }

    [CreateAssetMenu(
        fileName = "PdfContentCatalog",
        menuName = "ATLAS/SOS/Content Catalogs/PDF Content Catalog")]
    public sealed class PdfContentCatalog : ScriptableObject
    {
        [SerializeField]
        private List<PdfContentEntry> entries = new();

        public bool TryResolve(
            string contentId,
            out PdfContentEntry document,
            out string error)
        {
            document = null;
            error = null;

            if (string.IsNullOrWhiteSpace(contentId))
            {
                error = "The PDF file has no content ID.";
                return false;
            }

            PdfContentEntry match = null;

            if (entries != null)
            {
                foreach (PdfContentEntry entry in entries)
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
                            $"PDF content ID '{contentId}' is duplicated " +
                            "in the PDF catalog.";

                        return false;
                    }

                    match = entry;
                }
            }

            if (match == null)
            {
                error = $"PDF content '{contentId}' was not found.";
                return false;
            }

            if (!match.TryValidate(out error))
                return false;

            document = match;
            return true;
        }
    }
}