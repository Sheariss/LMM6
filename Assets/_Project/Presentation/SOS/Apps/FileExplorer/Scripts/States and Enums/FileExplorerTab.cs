using System.Collections.Generic;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerTab
    {
        private readonly List<string> history = new();

        private int historyIndex = -1;

        public int Id { get; }
        public string CurrentPath { get; private set; } = "";
        public string SearchQuery { get; set; } = "";
        public FileExplorerViewMode ViewMode { get; set; } =
            FileExplorerViewMode.Grid;
        public FileExplorerSortMode SortMode { get; set; } =
            FileExplorerSortMode.Name;
        public FileExplorerSortDirection SortDirection { get; set; } =
            FileExplorerSortDirection.Ascending;

        public bool CanGoBack =>
            historyIndex > 0;

        public bool CanGoForward =>
            historyIndex >= 0 &&
            historyIndex < history.Count - 1;

        public FileExplorerTab(
            int id,
            string initialPath)
        {
            Id = id;

            Navigate(initialPath);
        }

        public void Navigate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (historyIndex < history.Count - 1)
            {
                history.RemoveRange(
                    historyIndex + 1,
                    history.Count - historyIndex - 1);
            }

            history.Add(path);

            historyIndex =
                history.Count - 1;

            CurrentPath = path;

            SearchQuery = "";
        }

        public void GoBack()
        {
            if (!CanGoBack)
                return;

            historyIndex--;

            CurrentPath =
                history[historyIndex];

            SearchQuery = "";
        }

        public void GoForward()
        {
            if (!CanGoForward)
                return;

            historyIndex++;

            CurrentPath =
                history[historyIndex];

            SearchQuery = "";
        }
    }
}