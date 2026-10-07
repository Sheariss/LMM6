using System;
using System.Collections;
using Atlas.Core.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Settings
{
    public sealed class SettingsController : MonoBehaviour
    {
        [Header("Dependencies")]
        private UIDocument document;
        private SettingsManager settingsManager;

        [Header("Events")]
        [SerializeField] private UnityEvent closed = new();

        private SettingsBinder binder;
        private SettingsView view;
        private SettingsViewBuilder builder;
        private SettingsEngine engine;

        private SettingsSaveData draft;
        private SettingCategoryDefinition selectedCategory;

        public UnityEvent Closed => closed;

        private void OnEnable()
        {
            StartCoroutine(OpenNextFrame());
        }

        private IEnumerator OpenNextFrame()
        {
            // Allow UIDocument to create its visual tree.
            yield return null;
            Open();
        }

        public void Open()
        {
            if (!gameObject.activeInHierarchy)
                return;

            if (!enabled)
            {
                enabled = true;
                return;
            }

            StopAllCoroutines();

            // Already initialized for this opening.
            if (view != null)
            {
                view.Show();
                return;
            }

            try
            {
                if (document == null)
                    document = GetComponentInParent<UIDocument>();

                if (settingsManager == null)
                    settingsManager = SettingsManager.Instance;

                if (document == null || settingsManager == null)
                {
                    throw new InvalidOperationException(
                        "Settings requires a UIDocument and SettingsManager.");
                }

                binder = new SettingsBinder(document.rootVisualElement);

                if (!binder.IsValid)
                {
                    enabled = false;
                    return;
                }

                engine = settingsManager.Engine;
                draft = settingsManager.Current;

                view = new SettingsView(binder);

                builder = new SettingsViewBuilder(
                    view,
                    settingsManager.Catalog,
                    SelectCategory,
                    ChangeValue);

                view.SetHeader(
                    "Settings",
                    "Configure system preferences");

                binder.SearchField.SetValueWithoutNotify(string.Empty);

                builder.BuildNavigation();

                selectedCategory = null;

                foreach (var category in settingsManager.Catalog.Categories)
                {
                    if (category == null)
                        continue;

                    selectedCategory = category;
                    break;
                }

                binder.BackButton.clicked += Back;
                binder.ResetButton.clicked += ResetDraft;
                binder.ApplyButton.clicked += Apply;
                binder.DoneButton.clicked += Done;
                binder.SearchClearButton.clicked += ClearSearch;

                binder.SearchField.RegisterValueChangedCallback(
                    OnSearchChanged);

                RebuildContent();
                RefreshDirtyState();
                view.Show();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void SelectCategory(SettingCategoryDefinition category)
        {
            selectedCategory = category;
            RebuildContent();
        }

        private void RebuildContent()
        {
            builder.BuildCategory(
                selectedCategory,
                draft,
                binder.SearchField.value);
        }

        private void ChangeValue(SettingsSaveData.Entry proposed)
        {
            engine.SetValue(draft, proposed);

            view.RefreshValues(draft);
            RefreshDirtyState();
        }

        private void ResetDraft()
        {
            draft = engine.CreateDefaults();

            view.RefreshValues(draft);
            RefreshDirtyState();
        }

        private void Apply()
        {
            settingsManager.Apply(draft);
            draft = settingsManager.Current;

            view.RefreshValues(draft);
            RefreshDirtyState();
        }

        private void Done()
        {
            Apply();
            Close();
        }

        private void Back()
        {
            // Discard pending edits by closing without applying.
            Close();
        }

        private void Close()
        {
            // Disabling invokes OnDisable and releases the current UI.
            enabled = false;
            closed.Invoke();
        }

        public void Show()
        {
            // OnEnable will initialize the screen next frame.
            if (!gameObject.activeSelf)
            {
                enabled = true;
                gameObject.SetActive(true);
                return;
            }

            Open();
        }

        public void Hide()
        {
            if (!enabled)
                return;

            // OnDisable hides the view, removes callbacks,
            // and discards the current draft.
            enabled = false;
            closed.Invoke();
        }

        public void ResetView()
        {
            if (view == null || builder == null)
                return;

            binder.SearchField.SetValueWithoutNotify(string.Empty);

            selectedCategory = null;

            foreach (var category in settingsManager.Catalog.Categories)
            {
                if (category == null)
                    continue;

                selectedCategory = category;
                break;
            }

            RebuildContent();

            binder.NavigationScroll.scrollOffset = Vector2.zero;
            binder.ContentScroll.scrollOffset = Vector2.zero;
        }

        private void RefreshDirtyState()
        {
            bool dirty = !engine.AreEqual(
                draft,
                settingsManager.Current);

            view.SetDirty(dirty);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RebuildContent();
        }

        private void ClearSearch()
        {
            binder.SearchField.value = string.Empty;
        }

        private void OnDisable()
        {
            StopAllCoroutines();

            if (binder != null && binder.IsValid)
            {
                binder.BackButton.clicked -= Back;
                binder.ResetButton.clicked -= ResetDraft;
                binder.ApplyButton.clicked -= Apply;
                binder.DoneButton.clicked -= Done;
                binder.SearchClearButton.clicked -= ClearSearch;

                binder.SearchField.UnregisterValueChangedCallback(
                    OnSearchChanged);
            }

            if (view != null)
            {
                view.Hide();
                view.ClearNavigation();
                view.ClearContent();
            }

            binder = null;
            view = null;
            builder = null;
            engine = null;
            draft = null;
            selectedCategory = null;
        }
    }
}