using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.News
{
    public sealed class NewsSiteShellController : AppContentController
    {
        private NewsSiteShellBinder binder;
        private NewsSiteShellView view;
        private NewsSiteShellViewBuilder viewBuilder;
        private NewsSiteShellEngine engine;

        protected override void OnInitialize()
        {
            binder = new NewsSiteShellBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    $"{nameof(NewsSiteShellController)} failed to bind the News Site UI.");
            }

            engine = new NewsSiteShellEngine();
            viewBuilder = new NewsSiteShellViewBuilder();
            view = new NewsSiteShellView(binder);

            RegisterCallbacks();
            Render();
        }

        protected override void OnShutdown()
        {
            if (engine != null)
            {
                engine.StateChanged -= Render;
                engine.SectionChanged -= OnSectionChanged;
                engine.SearchRequested -= OnSearchRequested;
                engine.SignInRequested -= OnSignInRequested;
                engine.HomeRequested -= OnHomeRequested;
            }

            binder = null;
            view = null;
            viewBuilder = null;
            engine = null;
        }

        private void RegisterCallbacks()
        {
            BindClick(binder.LogoButton, OnLogoPressed);
            BindClick(binder.SearchButton, OnSearchPressed);
            BindClick(binder.SignInButton, OnSignInPressed);

            BindClick(binder.HomeButton, OnHomePressed);
            BindClick(binder.LocalButton, OnLocalPressed);
            BindClick(binder.PoliticsButton, OnPoliticsPressed);
            BindClick(binder.BusinessButton, OnBusinessPressed);
            BindClick(binder.TechnologyButton, OnTechnologyPressed);
            BindClick(binder.CultureButton, OnCulturePressed);
            BindClick(binder.WeatherButton, OnWeatherPressed);

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                OnSearchValueChanged);

            BindCallback<KeyDownEvent>(
                binder.SearchField,
                OnSearchKeyDown);

            engine.StateChanged += Render;
            engine.SectionChanged += OnSectionChanged;
            engine.SearchRequested += OnSearchRequested;
            engine.SignInRequested += OnSignInRequested;
            engine.HomeRequested += OnHomeRequested;
        }

        private void Render()
        {
            if (view == null || viewBuilder == null || engine == null)
                return;

            NewsSiteShellViewState state = viewBuilder.Build(engine);

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
            {
                return;
            }

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