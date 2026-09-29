namespace Atlas.Presentation.SOS.WebBrowser.News
{
    public sealed class NewsSiteShellViewBuilder
    {
        public NewsSiteShellViewState Build(NewsSiteShellEngine engine)
        {
            if (engine == null)
                return new NewsSiteShellViewState();

            return new NewsSiteShellViewState
            {
                DateText = engine.DateText,
                LocationText = engine.LocationText,
                WeatherText = engine.WeatherText,
                SearchText = engine.SearchText,
                BreakingNewsText = engine.BreakingNewsText,
                ShowBreakingNews = engine.ShowBreakingNews,
                ActiveSection = engine.ActiveSection
            };
        }
    }
}