using System;

namespace Atlas.Presentation.SOS.WebBrowser.News
{
    public sealed class NewsSiteShellEngine
    {
        public string DateText { get; private set; } = "Sunday, June 12, 2026";
        public string LocationText { get; private set; } = "San Juan, PR";
        public string WeatherText { get; private set; } = "64°F";
        public string SearchText { get; private set; } = string.Empty;

        public string BreakingNewsText { get; private set; } =
            "Police continue search as investigation enters third day     •     " +
            "Severe weather expected across northern Puerto Rico this afternoon     •     " +
            "Airport officials report delays at Luis Muñoz Marín International Airport";

        public bool ShowBreakingNews { get; private set; } = true;
        public NewsSection ActiveSection { get; private set; } = NewsSection.Home;

        public event Action StateChanged;
        public event Action<NewsSection> SectionChanged;
        public event Action<string> SearchRequested;
        public event Action SignInRequested;
        public event Action HomeRequested;

        public void NavigateTo(NewsSection section)
        {
            if (ActiveSection == section)
                return;

            ActiveSection = section;

            SectionChanged?.Invoke(section);
            StateChanged?.Invoke();
        }

        public void NavigateHome()
        {
            bool changed = ActiveSection != NewsSection.Home;

            ActiveSection = NewsSection.Home;

            HomeRequested?.Invoke();

            if (changed)
                SectionChanged?.Invoke(NewsSection.Home);

            StateChanged?.Invoke();
        }

        public void SetSearchText(string text)
        {
            text ??= string.Empty;

            if (SearchText == text)
                return;

            SearchText = text;
            StateChanged?.Invoke();
        }

        public void Search(string query)
        {
            query = query?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(query))
                return;

            SearchText = query;

            SearchRequested?.Invoke(query);
            StateChanged?.Invoke();
        }

        public void RequestSignIn()
        {
            SignInRequested?.Invoke();
        }

        public void SetUtilityInformation(
            string date,
            string location,
            string weather)
        {
            DateText = date ?? string.Empty;
            LocationText = location ?? string.Empty;
            WeatherText = weather ?? string.Empty;

            StateChanged?.Invoke();
        }

        public void SetBreakingNews(string text)
        {
            BreakingNewsText = text ?? string.Empty;
            ShowBreakingNews = !string.IsNullOrWhiteSpace(BreakingNewsText);

            StateChanged?.Invoke();
        }

        public void ShowBreakingNewsBar()
        {
            if (ShowBreakingNews)
                return;

            ShowBreakingNews = true;
            StateChanged?.Invoke();
        }

        public void HideBreakingNewsBar()
        {
            if (!ShowBreakingNews)
                return;

            ShowBreakingNews = false;
            StateChanged?.Invoke();
        }
    }
}