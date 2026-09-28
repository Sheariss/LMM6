namespace Atlas.Presentation.SOS.WebBrowser
{
    public sealed class WebBrowserViewBuilder
    {
        public WebBrowserViewState Build(WebBrowserEngine engine)
        {
            if (engine == null)
                return new WebBrowserViewState();

            string address = engine.CurrentAddress;

            return new WebBrowserViewState
            {
                TabTitle = BuildTabTitle(address),
                CanCloseTab = true,

                CanGoBack = engine.CanGoBack,
                CanGoForward = engine.CanGoForward,
                IsLoading = false,

                Address = address,
                IsBookmarked = engine.IsCurrentPageBookmarked,

                ShowBookmarksBar = true,
                ShowBookmarksOverflow = false,

                ShowPage = true
            };
        }

        private static string BuildTabTitle(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "New Tab";

            return address switch
            {
                "ashfordherald.com" => "Ashford Herald",
                "ashfordpd.gov" => "Ashford Police Department",
                "ashfordmn.gov" => "Town of Ashford",
                _ => address
            };
        }
    }
}