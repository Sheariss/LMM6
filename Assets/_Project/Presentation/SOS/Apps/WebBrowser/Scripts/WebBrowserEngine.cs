using System;
using System.Collections.Generic;

namespace Atlas.Presentation.SOS.WebBrowser
{
    public sealed class WebBrowserEngine
    {
        private readonly List<string> history = new();
        private readonly HashSet<string> bookmarks = new();

        private int historyIndex = -1;

        public string CurrentAddress =>
            historyIndex >= 0 && historyIndex < history.Count
                ? history[historyIndex]
                : "";

        public bool CanGoBack => historyIndex > 0;
        public bool CanGoForward => historyIndex >= 0 && historyIndex < history.Count - 1;
        public bool IsCurrentPageBookmarked => bookmarks.Contains(CurrentAddress);

        public void Navigate(string address)
        {
            address = NormalizeAddress(address);

            if (string.IsNullOrWhiteSpace(address))
                return;

            if (historyIndex < history.Count - 1)
            {
                history.RemoveRange(
                    historyIndex + 1,
                    history.Count - historyIndex - 1);
            }

            history.Add(address);
            historyIndex = history.Count - 1;
        }

        public void GoBack()
        {
            if (!CanGoBack)
                return;

            historyIndex--;
        }

        public void GoForward()
        {
            if (!CanGoForward)
                return;

            historyIndex++;
        }

        public void Reload()
        {
            if (string.IsNullOrWhiteSpace(CurrentAddress))
                return;

            // The current page does not change.
            // The controller can simply rebuild/reload its content.
        }

        public void ToggleBookmark()
        {
            if (string.IsNullOrWhiteSpace(CurrentAddress))
                return;

            if (!bookmarks.Add(CurrentAddress))
                bookmarks.Remove(CurrentAddress);
        }

        public bool IsBookmarked(string address)
        {
            address = NormalizeAddress(address);

            return bookmarks.Contains(address);
        }

        private static string NormalizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "";

            address = address.Trim();

            if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                address = address.Substring(7);

            if (address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                address = address.Substring(8);

            return address.TrimEnd('/');
        }
    }
}