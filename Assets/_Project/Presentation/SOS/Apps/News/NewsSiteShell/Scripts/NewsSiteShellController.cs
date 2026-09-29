using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser.News
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class NewsSiteShellController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private NewsSiteShellBinder binder;
        private NewsSiteShellView view;
        private NewsSiteShellViewBuilder viewBuilder;
        private NewsSiteShellEngine engine;

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        private void Initialize()
        {
            uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError(
                    $"{nameof(NewsSiteShellController)} requires a UIDocument.",
                    this);

                return;
            }

            binder = new NewsSiteShellBinder(
                uiDocument.rootVisualElement);

            if (!binder.IsValid)
            {
                Debug.LogError(
                    $"{nameof(NewsSiteShellController)} failed to bind the News Site UI.",
                    this);

                return;
            }

            engine = new NewsSiteShellEngine();
            viewBuilder = new NewsSiteShellViewBuilder();
            view = new NewsSiteShellView(binder);

            RegisterCallbacks();
            Render();
        }

        private void RegisterCallbacks()
        {
            binder.LogoButton.clicked += OnLogoPressed;
            binder.SearchButton.clicked += OnSearchPressed;
            binder.SignInButton.clicked += OnSignInPressed;

            binder.HomeButton.clicked += OnHomePressed;
            binder.LocalButton.clicked += OnLocalPressed;
            binder.PoliticsButton.clicked += OnPoliticsPressed;
            binder.BusinessButton.clicked += OnBusinessPressed;
            binder.TechnologyButton.clicked += OnTechnologyPressed;
            binder.CultureButton.clicked += OnCulturePressed;
            binder.WeatherButton.clicked += OnWeatherPressed;

            binder.SearchField.RegisterValueChangedCallback(
                OnSearchValueChanged);

            binder.SearchField.RegisterCallback<KeyDownEvent>(
                OnSearchKeyDown);

            engine.StateChanged += Render;
            engine.SectionChanged += OnSectionChanged;
            engine.SearchRequested += OnSearchRequested;
            engine.SignInRequested += OnSignInRequested;
            engine.HomeRequested += OnHomeRequested;
        }

        private void UnregisterCallbacks()
        {
            if (binder != null)
            {
                binder.LogoButton.clicked -= OnLogoPressed;
                binder.SearchButton.clicked -= OnSearchPressed;
                binder.SignInButton.clicked -= OnSignInPressed;

                binder.HomeButton.clicked -= OnHomePressed;
                binder.LocalButton.clicked -= OnLocalPressed;
                binder.PoliticsButton.clicked -= OnPoliticsPressed;
                binder.BusinessButton.clicked -= OnBusinessPressed;
                binder.TechnologyButton.clicked -= OnTechnologyPressed;
                binder.CultureButton.clicked -= OnCulturePressed;
                binder.WeatherButton.clicked -= OnWeatherPressed;

                binder.SearchField.UnregisterValueChangedCallback(
                    OnSearchValueChanged);

                binder.SearchField.UnregisterCallback<KeyDownEvent>(
                    OnSearchKeyDown);
            }

            if (engine != null)
            {
                engine.StateChanged -= Render;
                engine.SectionChanged -= OnSectionChanged;
                engine.SearchRequested -= OnSearchRequested;
                engine.SignInRequested -= OnSignInRequested;
                engine.HomeRequested -= OnHomeRequested;
            }
        }

        private void Render()
        {
            if (view == null || viewBuilder == null || engine == null)
                return;

            NewsSiteShellViewState state =
                viewBuilder.Build(engine);

            view.Render(state);
        }

        private void OnLogoPressed()
        {
            engine.NavigateHome();
        }

        private void OnSearchPressed()
        {
            engine.Search(binder.SearchField.value);
        }

        private void OnSignInPressed()
        {
            engine.RequestSignIn();
        }

        private void OnHomePressed()
        {
            engine.NavigateHome();
        }

        private void OnLocalPressed()
        {
            engine.NavigateTo(NewsSection.Local);
        }

        private void OnPoliticsPressed()
        {
            engine.NavigateTo(NewsSection.Politics);
        }

        private void OnBusinessPressed()
        {
            engine.NavigateTo(NewsSection.Business);
        }

        private void OnTechnologyPressed()
        {
            engine.NavigateTo(NewsSection.Technology);
        }

        private void OnCulturePressed()
        {
            engine.NavigateTo(NewsSection.Culture);
        }

        private void OnWeatherPressed()
        {
            engine.NavigateTo(NewsSection.Weather);
        }

        private void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            engine.SetSearchText(evt.newValue);
        }

        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
                return;

            engine.Search(binder.SearchField.value);

            evt.StopPropagation();
        }

        private void OnSectionChanged(NewsSection section)
        {
            Debug.Log($"News section changed to {section}.", this);

            // Later:
            // LoadSection(section);
        }

        private void OnSearchRequested(string query)
        {
            Debug.Log($"News search requested: {query}", this);

            // Later:
            // LoadSearchResults(query);
        }

        private void OnSignInRequested()
        {
            Debug.Log("News sign-in requested.", this);

            // Later:
            // ShowSignInPage();
        }

        private void OnHomeRequested()
        {
            Debug.Log("News home requested.", this);

            // Later:
            // LoadHomePage();
        }
    }
}