using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Panels
{
    public sealed class PanelController : MonoBehaviour
    {
        [Serializable]
        private sealed class Registration
        {
            public DesktopPanelID id;
            public PanelRuntime runtimePrefab;
        }

        private sealed class Instance
        {
            public PanelRuntime Prefab;
            public PanelRuntime Runtime;
            public Button Button;
            public Action ClickHandler;
        }

        [SerializeField]
        private List<Registration> panels = new();

        private readonly Dictionary<DesktopPanelID, Instance>
            instances = new();

        private VisualElement documentRoot;
        private VisualElement panelLayer;

        private Func<bool> canUse;
        private Func<string, bool> canAccessApp;
        private Action<string> launchApp;

        private Instance active;
        private int escapeClaimedFrame = -1;

        public bool HasOpenPanel => active != null;

        // The UI event can close the panel before the pause menu polls the same
        // key. Retain ownership for that frame so one press cannot do both.
        public bool HasEscapePriority =>
            HasOpenPanel || escapeClaimedFrame == Time.frameCount;

        public event Action PanelOpening;

        public void Initialize(
            VisualElement documentRoot,
            VisualElement panelLayer,
            Func<bool> canUse,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "PanelController must be active.");
            }

            this.documentRoot = documentRoot ??
                throw new ArgumentNullException(nameof(documentRoot));

            this.panelLayer = panelLayer ??
                throw new ArgumentNullException(nameof(panelLayer));

            this.canUse = canUse ??
                throw new ArgumentNullException(nameof(canUse));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            try
            {
                foreach (Registration registration in panels)
                {
                    if (registration == null ||
                        registration.runtimePrefab == null)
                    {
                        throw new InvalidOperationException(
                            "A panel registration has no runtime prefab.");
                    }

                    if (instances.ContainsKey(registration.id))
                    {
                        throw new InvalidOperationException(
                            $"Panel '{registration.id}' is registered twice.");
                    }

                    instances.Add(
                        registration.id,
                        new Instance
                        {
                            Prefab = registration.runtimePrefab
                        });
                }

                panelLayer.pickingMode = PickingMode.Ignore;

                documentRoot.RegisterCallback<PointerDownEvent>(
                    OnPointerDown,
                    TrickleDown.TrickleDown);

                documentRoot.RegisterCallback<KeyDownEvent>(
                    OnKeyDownCapture,
                    TrickleDown.TrickleDown);
                documentRoot.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public void BindButton(DesktopPanelID id, Button button)
        {
            if (!instances.TryGetValue(id, out Instance instance))
            {
                throw new InvalidOperationException(
                    $"Panel '{id}' is not registered.");
            }

            if (button == null)
                throw new ArgumentNullException(nameof(button));

            if (instance.Button != null)
                instance.Button.clicked -= instance.ClickHandler;

            instance.Button = button;
            instance.ClickHandler = () => Toggle(id);

            button.clicked += instance.ClickHandler;
        }

        public void Toggle(DesktopPanelID id)
        {
            if (canUse == null || !canUse())
            {
                Close();
                return;
            }

            if (!instances.TryGetValue(id, out Instance instance))
                return;

            if (ReferenceEquals(active, instance))
            {
                Close(restoreFocus: true);
                return;
            }

            try
            {
                EnsureRuntime(instance);

                Close();
                PanelOpening?.Invoke();

                active = instance;
                active.Runtime.Show();
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(active, instance))
                    Close();

                Release(instance);
                Debug.LogException(exception, this);
            }
        }

        public void Close(bool restoreFocus = false)
        {
            Instance previous = active;
            active = null;

            if (previous == null)
                return;

            try
            {
                if (previous.Runtime != null)
                    previous.Runtime.Hide();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (restoreFocus)
                previous.Button?.Focus();
        }

        private void EnsureRuntime(Instance instance)
        {
            if (instance.Runtime != null)
                return;

            instance.Runtime = Instantiate(instance.Prefab, transform);

            try
            {
                instance.Runtime.Initialize(
                    panelLayer,
                    canAccessApp,
                    launchApp);

                if (instance.Runtime.Surface == null)
                {
                    throw new InvalidOperationException(
                        "The panel runtime did not provide a Surface.");
                }

                instance.Runtime.Hide();
                instance.Runtime.CloseRequested += OnCloseRequested;
            }
            catch
            {
                Release(instance);
                throw;
            }
        }

        private void OnCloseRequested(PanelRuntime runtime)
        {
            if (active != null && active.Runtime == runtime)
                Close();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (active == null)
                return;

            if (!canUse())
            {
                Close();
                return;
            }

            var target = evt.target as VisualElement;

            if (target == null || active.Runtime.Contains(target))
                return;

            Button button = active.Button;

            if (button != null &&
                (target == button || button.Contains(target)))
            {
                return;
            }

            Close();
        }

        private void OnKeyDownCapture(KeyDownEvent evt)
        {
            if (active != null && evt.keyCode == KeyCode.Escape)
                escapeClaimedFrame = Time.frameCount;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (active == null || evt.keyCode != KeyCode.Escape)
                return;

            Close(restoreFocus: true);
            evt.StopPropagation();
        }

        public void Shutdown()
        {
            Close();

            if (documentRoot != null)
            {
                documentRoot.UnregisterCallback<PointerDownEvent>(
                    OnPointerDown,
                    TrickleDown.TrickleDown);

                documentRoot.UnregisterCallback<KeyDownEvent>(OnKeyDown);
                documentRoot.UnregisterCallback<KeyDownEvent>(
                    OnKeyDownCapture,
                    TrickleDown.TrickleDown);
            }

            foreach (Instance instance in instances.Values)
            {
                if (instance.Button != null)
                    instance.Button.clicked -= instance.ClickHandler;

                Release(instance);
            }

            instances.Clear();

            documentRoot = null;
            panelLayer = null;
            canUse = null;
            canAccessApp = null;
            launchApp = null;
        }

        private void Release(Instance instance)
        {
            PanelRuntime runtime = instance.Runtime;
            instance.Runtime = null;

            if (runtime == null)
                return;

            runtime.CloseRequested -= OnCloseRequested;

            try
            {
                runtime.Shutdown();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                Destroy(runtime.gameObject);
            }
        }

        private void OnDisable()
        {
            Shutdown();
        }
    }
}
