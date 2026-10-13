using Atlas.Presentation.SOS.Apps;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.Websites
{
    [Serializable]
    public sealed class WebPageDefinition
    {
        [Header("Identity")]
        [SerializeField] private string pageID;
        [SerializeField] private string path = "/home";

        [Header("Content")]
        [SerializeField] private VisualTreeAsset pageUXML;
        [SerializeField] private AppContentController runtimePrefab;

        // Optional: identifies data rendered by a shared controller.
        // For example, multiple article pages can use the same
        // controller and UXML, with different content IDs.
        [SerializeField] private string contentID;

        [Header("Browser Metadata")]
        [SerializeField] private string tabTitle;
        [SerializeField] private string bookmarkText;
        [SerializeField] private Sprite faviconOverride;

        public string PageID => pageID;
        public string Path => path;

        public VisualTreeAsset PageUXML => pageUXML;
        public AppContentController RuntimePrefab => runtimePrefab;
        public string ContentID => contentID;

        public string TabTitle => tabTitle;
        public string BookmarkText => bookmarkText;
        public Sprite FaviconOverride => faviconOverride;
    }

    [Serializable]
    public sealed class WebsiteDefinition
    {
        [Header("Identity")]
        [SerializeField] private string websiteID;
        [SerializeField] private string displayName;

        // Host only, for example: ashfordherald.com
        // The router will handle schemes, paths, and query strings.
        [SerializeField] private string domain;
        [SerializeField] private string homePath = "/home";

        [Header("Browser Defaults")]
        [SerializeField] private Sprite favicon;

        [Header("Shared Shell (Optional)")]
        [SerializeField] private VisualTreeAsset shellUXML;
        [SerializeField] private AppContentController shellRuntimePrefab;

        [Header("Pages")]
        [SerializeField]
        private List<WebPageDefinition> pages = new();

        public string WebsiteID => websiteID;
        public string DisplayName => displayName;
        public string Domain => domain;
        public string HomePath => homePath;

        public Sprite Favicon => favicon;

        public VisualTreeAsset ShellUXML => shellUXML;
        public AppContentController ShellRuntimePrefab =>
            shellRuntimePrefab;

        public IReadOnlyList<WebPageDefinition> Pages => pages;

        public bool TryGetPage(
            string pageID,
            out WebPageDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(pageID))
                return false;

            foreach (WebPageDefinition page in pages)
            {
                if (page != null &&
                    string.Equals(
                        page.PageID,
                        pageID,
                        StringComparison.Ordinal))
                {
                    definition = page;
                    return true;
                }
            }

            return false;
        }

        // Expects a path already normalized by the router.
        public bool TryGetPageByPath(
            string path,
            out WebPageDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(path))
                return false;

            foreach (WebPageDefinition page in pages)
            {
                if (page != null &&
                    string.Equals(
                        page.Path,
                        path,
                        StringComparison.Ordinal))
                {
                    definition = page;
                    return true;
                }
            }

            return false;
        }

        public string GetTabTitle(WebPageDefinition page)
        {
            return !string.IsNullOrWhiteSpace(page?.TabTitle)
                ? page.TabTitle
                : displayName;
        }

        public string GetBookmarkText(WebPageDefinition page)
        {
            return !string.IsNullOrWhiteSpace(page?.BookmarkText)
                ? page.BookmarkText
                : GetTabTitle(page);
        }

        public Sprite GetFavicon(WebPageDefinition page)
        {
            return page?.FaviconOverride != null
                ? page.FaviconOverride
                : favicon;
        }
    }

    [CreateAssetMenu(
        fileName = "Website Catalog",
        menuName = "ATLAS/Website Catalog")]
    public sealed class WebsiteCatalog : ScriptableObject
    {
        [SerializeField]
        private List<WebsiteDefinition> websites = new();

        public IReadOnlyList<WebsiteDefinition> Websites => websites;

        public bool TryGetWebsite(
            string websiteID,
            out WebsiteDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(websiteID))
                return false;

            foreach (WebsiteDefinition website in websites)
            {
                if (website != null &&
                    string.Equals(
                        website.WebsiteID,
                        websiteID,
                        StringComparison.Ordinal))
                {
                    definition = website;
                    return true;
                }
            }

            return false;
        }

        // Expects a host already extracted by the router.
        public bool TryGetWebsiteByDomain(
            string domain,
            out WebsiteDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(domain))
                return false;

            foreach (WebsiteDefinition website in websites)
            {
                if (website != null &&
                    string.Equals(
                        website.Domain,
                        domain,
                        StringComparison.OrdinalIgnoreCase))
                {
                    definition = website;
                    return true;
                }
            }

            return false;
        }
    }
}