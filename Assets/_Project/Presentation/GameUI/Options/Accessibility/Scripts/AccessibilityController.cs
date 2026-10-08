using Atlas.Core.Accessibility;
using Atlas.Presentation.ColorPicker;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Accessibility
{
    public sealed class AccessibilityController : MonoBehaviour
    {
        [Header("Dependencies")]
        private UIDocument document;
        private AccessibilityManager accessibilityManager;
        [SerializeField] private ColorPickerController colorPicker;

        [Header("Events")]
        [SerializeField] private UnityEvent closed = new();

        private AccessibilityBinder binder;
        private AccessibilityView view;
        private AccessibilityViewBuilder builder;
        private AccessibilityEngine engine;

        private AccessibilitySaveData draft;
        private AccessibilityCategoryDefinition selectedCategory;

        public UnityEvent Closed => closed;

        private void OnEnable()
        {
            StartCoroutine(OpenNextFrame());
        }

        private IEnumerator OpenNextFrame()
        {
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

            if (view != null)
            {
                view.Show();
                return;
            }

            try
            {
                if (document == null)
                    document = GetComponentInParent<UIDocument>();

                if (accessibilityManager == null)
                    accessibilityManager = AccessibilityManager.Instance;

                if (document == null || accessibilityManager == null)
                {
                    throw new InvalidOperationException(
                        "Accessibility requires a UIDocument and AccessibilityManager.");
                }

                binder = new AccessibilityBinder(document.rootVisualElement);

                if (!binder.IsValid)
                {
                    enabled = false;
                    return;
                }

                engine = accessibilityManager.Engine;
                draft = accessibilityManager.Current;

                view = new AccessibilityView(binder);
                builder = new AccessibilityViewBuilder(
                    view,
                    accessibilityManager.Catalog,
                    SelectCategory,
                    ChangeValue,
                    OpenColorPicker);

                view.SetHeader(
                    "Accessibility",
                    "Customize controls, visuals, audio, and assistance to suit your needs.");

                binder.SearchField.SetValueWithoutNotify(string.Empty);

                builder.BuildNavigation();
                selectedCategory = GetFirstCategory();

                binder.BackButton.clicked += Back;
                binder.ResetButton.clicked += ResetDraft;
                binder.ApplyButton.clicked += Apply;
                binder.DoneButton.clicked += Done;
                binder.SearchClearButton.clicked += ClearSearch;
                binder.SearchField.RegisterValueChangedCallback(OnSearchChanged);

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

        private void OpenColorPicker(
            AccessibilitySettingDefinition definition,
            Color current,
            VisualElement opener)
        {
            if (colorPicker == null)
            {
                Debug.LogError("Assign the ColorPickerController in the Inspector.", this);
                return;
            }

            colorPicker.Open(
                current,
                definition.AllowAlpha,
                definition.Label,
                opener);
        }

        private AccessibilityCategoryDefinition GetFirstCategory()
        {
            foreach (var category in accessibilityManager.Catalog.Categories)
            {
                if (category != null)
                    return category;
            }

            return null;
        }

        private void SelectCategory(AccessibilityCategoryDefinition category)
        {
            selectedCategory = category;
            binder.SearchField.SetValueWithoutNotify(string.Empty);
            RebuildContent();
        }

        private void RebuildContent()
        {
            builder.BuildContent(
                selectedCategory,
                draft,
                binder.SearchField.value);
        }

        private void ChangeValue(AccessibilitySaveData.Entry proposed)
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
            accessibilityManager.Apply(draft);
            draft = accessibilityManager.Current;

            view.RefreshValues(draft);
            RefreshDirtyState();
        }

        private void Done()
        {
            Apply();
            Hide();
        }

        private void Back()
        {
            Hide();
        }

        public void Show()
        {
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

            enabled = false;
            closed.Invoke();
        }

        public void ResetView()
        {
            if (view == null)
                return;

            binder.SearchField.SetValueWithoutNotify(string.Empty);
            selectedCategory = GetFirstCategory();

            RebuildContent();

            binder.NavigationScroll.scrollOffset = Vector2.zero;
            binder.ContentScroll.scrollOffset = Vector2.zero;
        }

        private void RefreshDirtyState()
        {
            view.SetDirty(!engine.AreEqual(
                draft,
                accessibilityManager.Current));
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RebuildContent();
        }

        private void ClearSearch()
        {
            binder.SearchField.SetValueWithoutNotify(string.Empty);
            RebuildContent();
            binder.SearchField.Focus();
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