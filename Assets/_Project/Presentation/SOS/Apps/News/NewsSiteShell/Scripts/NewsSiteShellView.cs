using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser.News
{
    public enum NewsSection
    {
        Home,
        Local,
        Politics,
        Business,
        Technology,
        Culture,
        Weather
    }

    public sealed class NewsSiteShellViewState
    {
        public string DateText { get; set; }
        public string LocationText { get; set; }
        public string WeatherText { get; set; }
        public string SearchText { get; set; }
        public string BreakingNewsText { get; set; }
        public bool ShowBreakingNews { get; set; }
        public NewsSection ActiveSection { get; set; }
    }

    public sealed class NewsSiteShellView
    {
        private const string ActiveNavigationClass = "news-nav__button--active";
        private readonly NewsSiteShellBinder binder;

        public NewsSiteShellView(NewsSiteShellBinder binder)
        {
            this.binder = binder;
        }

        public void Render(NewsSiteShellViewState state)
        {
            if (state == null)
                return;

            SetUtilityInformation(
                state.DateText,
                state.LocationText,
                state.WeatherText);

            SetSearchText(state.SearchText);
            SetBreakingNews(state.BreakingNewsText, state.ShowBreakingNews);
            SetActiveSection(state.ActiveSection);
        }

        public void SetUtilityInformation(string date, string location, string weather)
        {
            if (binder.DateLabel != null)
                binder.DateLabel.text = date ?? string.Empty;

            if (binder.LocationLabel != null)
                binder.LocationLabel.text = location ?? string.Empty;

            if (binder.WeatherLabel != null)
                binder.WeatherLabel.text = weather ?? string.Empty;
        }

        public void SetSearchText(string text)
        {
            if (binder.SearchField == null)
                return;

            if (binder.SearchField.value != text)
                binder.SearchField.SetValueWithoutNotify(text ?? string.Empty);
        }

        public void SetBreakingNews(string text, bool visible)
        {
            if (binder.BreakingNewsBar != null)
                binder.BreakingNewsBar.style.display =
                    visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (binder.BreakingNewsText != null)
                binder.BreakingNewsText.text = text ?? string.Empty;
        }

        public void SetActiveSection(NewsSection section)
        {
            ClearNavigationState();

            Button activeButton = GetSectionButton(section);

            activeButton?.AddToClassList(ActiveNavigationClass);
        }

        public void ClearContent()
        {
            binder.ContentContainer?.Clear();
        }

        public void SetContent(VisualElement content)
        {
            if (binder.ContentContainer == null)
                return;

            binder.ContentContainer.Clear();

            if (content != null)
                binder.ContentContainer.Add(content);
        }

        private void ClearNavigationState()
        {
            binder.HomeButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.LocalButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.PoliticsButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.BusinessButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.TechnologyButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.CultureButton?.RemoveFromClassList(ActiveNavigationClass);
            binder.WeatherButton?.RemoveFromClassList(ActiveNavigationClass);
        }

        private Button GetSectionButton(NewsSection section)
        {
            return section switch
            {
                NewsSection.Home => binder.HomeButton,
                NewsSection.Local => binder.LocalButton,
                NewsSection.Politics => binder.PoliticsButton,
                NewsSection.Business => binder.BusinessButton,
                NewsSection.Technology => binder.TechnologyButton,
                NewsSection.Culture => binder.CultureButton,
                NewsSection.Weather => binder.WeatherButton,
                _ => binder.HomeButton
            };
        }
    }
}