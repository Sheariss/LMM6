using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser.News
{
    public sealed class NewsSiteShellBinder : UIBinder
    {
        public VisualElement Root { get; }
        public VisualElement UtilityBar { get; }
        public Label DateLabel { get; }
        public Label LocationLabel { get; }
        public Label WeatherLabel { get; }
        public VisualElement BrandBar { get; }
        public Button LogoButton { get; }
        public VisualElement SearchGroup { get; }
        public TextField SearchField { get; }
        public Button SearchButton { get; }
        public Button SignInButton { get; }
        public VisualElement NavigationBar { get; }
        public Button HomeButton { get; }
        public Button LocalButton { get; }
        public Button PoliticsButton { get; }
        public Button BusinessButton { get; }
        public Button TechnologyButton { get; }
        public Button CultureButton { get; }
        public Button WeatherButton { get; }
        public VisualElement BreakingNewsBar { get; }
        public VisualElement BreakingNewsViewport { get; }
        public VisualElement BreakingNewsTrack { get; }
        public Label BreakingNewsText { get; }
        public VisualElement ContentContainer { get; }

        public NewsSiteShellBinder(VisualElement root)
        {
            Root = Bind<VisualElement>(root, "NewsSiteRoot");
            UtilityBar = Bind<VisualElement>(root, "NewsUtilityBar");
            DateLabel = Bind<Label>(root, "NewsDateLabel");
            LocationLabel = Bind<Label>(root, "NewsLocationLabel");
            WeatherLabel = Bind<Label>(root, "NewsWeatherLabel");
            BrandBar = Bind<VisualElement>(root, "NewsBrandBar");
            LogoButton = Bind<Button>(root, "NewsLogoButton");
            SearchGroup = Bind<VisualElement>(root, "NewsSearchGroup");
            SearchField = Bind<TextField>(root, "NewsSearchField");
            SearchButton = Bind<Button>(root, "NewsSearchButton");
            SignInButton = Bind<Button>(root, "NewsSignInButton");
            NavigationBar = Bind<VisualElement>(root, "NewsNavigationBar");
            HomeButton = Bind<Button>(root, "NewsHomeButton");
            LocalButton = Bind<Button>(root, "NewsLocalButton");
            PoliticsButton = Bind<Button>(root, "NewsPoliticsButton");
            BusinessButton = Bind<Button>(root, "NewsBusinessButton");
            TechnologyButton = Bind<Button>(root, "NewsTechnologyButton");
            CultureButton = Bind<Button>(root, "NewsCultureButton");
            WeatherButton = Bind<Button>(root, "NewsWeatherButton");
            BreakingNewsBar = Bind<VisualElement>(root, "BreakingNewsBar");
            BreakingNewsViewport = Bind<VisualElement>(root, "BreakingNewsViewport");
            BreakingNewsTrack = Bind<VisualElement>(root, "BreakingNewsTrack");
            BreakingNewsText = Bind<Label>(root, "BreakingNewsText");
            ContentContainer = Bind<VisualElement>(root, "NewsContentContainer");
        }
    }
}