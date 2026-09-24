using Atlas.Presentation.SOS.Windows;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Windows
{
    public sealed class WindowController : MonoBehaviour
    {
        private static WindowController focusedWindow;

        private UIDocument uiDocument;

        private WindowBinder binder;
        private WindowView view;

        public WindowState State { get; private set; } = WindowState.Normal;

        public bool IsFocused { get; private set; }

        private Rect restoredBounds;

        private void Start()
        {
            uiDocument = GetComponentInParent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError(
                    "[WindowController] UIDocument was not found.");

                return;
            }

            binder = new WindowBinder(
                uiDocument.rootVisualElement);

            if (!binder.IsValid)
                return;

            view = new WindowView(binder);

            RegisterCallbacks();

            Focus();
        }

        private void RegisterCallbacks()
        {
            binder.Root.RegisterCallback<PointerDownEvent>(
                OnWindowPointerDown);

            binder.MinimizeButton.clicked += OnMinimizePressed;

            binder.MaximizeButton.clicked += OnMaximizePressed;

            binder.CloseButton.clicked += OnClosePressed;
        }

        private void OnDestroy()
        {
            if (binder == null)
                return;

            binder.Root.UnregisterCallback<PointerDownEvent>(
                OnWindowPointerDown);

            binder.MinimizeButton.clicked -= OnMinimizePressed;

            binder.MaximizeButton.clicked -= OnMaximizePressed;

            binder.CloseButton.clicked -= OnClosePressed;

            if (focusedWindow == this)
                focusedWindow = null;
        }

        // --------------------------------------------------
        // FOCUS
        // --------------------------------------------------

        private void OnWindowPointerDown(PointerDownEvent evt)
        {
            Focus();
        }

        public void Focus()
        {
            if (State == WindowState.Minimized)
                return;

            if (focusedWindow == this)
                return;

            focusedWindow?.Unfocus();

            focusedWindow = this;

            IsFocused = true;

            binder.Root.BringToFront();

            view.SetFocused(true);
        }

        public void Unfocus()
        {
            IsFocused = false;

            view.SetFocused(false);
        }

        // --------------------------------------------------
        // MINIMIZE
        // --------------------------------------------------

        private void OnMinimizePressed()
        {
            Minimize();
        }

        public void Minimize()
        {
            if (State == WindowState.Minimized)
                return;

            State = WindowState.Minimized;

            IsFocused = false;

            if (focusedWindow == this)
                focusedWindow = null;

            view.SetFocused(false);
            view.Hide();
        }

        public void RestoreFromMinimized()
        {
            if (State != WindowState.Minimized)
                return;

            State = WindowState.Normal;

            view.Show();

            Focus();
        }

        // --------------------------------------------------
        // MAXIMIZE / RESTORE
        // --------------------------------------------------

        private void OnMaximizePressed()
        {
            ToggleMaximize();
        }

        public void ToggleMaximize()
        {
            if (State == WindowState.Maximized)
            {
                Restore();
            }
            else
            {
                Maximize();
            }
        }

        public void Maximize()
        {
            if (State == WindowState.Maximized)
                return;

            SaveCurrentBounds();

            State = WindowState.Maximized;

            binder.Root.style.left = 0;
            binder.Root.style.top = 0;

            binder.Root.style.width =
                Length.Percent(100);

            binder.Root.style.height =
                Length.Percent(100);

            view.SetMaximized(true);

            Focus();
        }

        public void Restore()
        {
            if (State != WindowState.Maximized)
                return;

            State = WindowState.Normal;

            binder.Root.style.left =
                restoredBounds.x;

            binder.Root.style.top =
                restoredBounds.y;

            binder.Root.style.width =
                restoredBounds.width;

            binder.Root.style.height =
                restoredBounds.height;

            view.SetMaximized(false);

            Focus();
        }

        private void SaveCurrentBounds()
        {
            restoredBounds = new Rect(
                binder.Root.resolvedStyle.left,
                binder.Root.resolvedStyle.top,
                binder.Root.resolvedStyle.width,
                binder.Root.resolvedStyle.height);
        }

        // --------------------------------------------------
        // CLOSE
        // --------------------------------------------------

        private void OnClosePressed()
        {
            binder.Root.style.display =
                DisplayStyle.None;
        }
    }
}