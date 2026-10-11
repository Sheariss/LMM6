using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Windows
{
    public sealed class WindowController : MonoBehaviour, IAppWindow
    {
        private static WindowController focusedWindow;

        private VisualElement instanceRoot;
        private WindowBinder binder;
        private WindowView view;

        private bool initialized;
        private bool disposed;
        private bool closeRequested;
        private bool destroying;
        private bool isResizable;

        private WindowState stateBeforeMinimize = WindowState.Normal;

        private StyleLength restoredLeft;
        private StyleLength restoredTop;
        private StyleLength restoredWidth;
        private StyleLength restoredHeight;
        private StyleLength restoredMinWidth;
        private StyleLength restoredMinHeight;

        private bool isDragging;
        private int dragPointerId = -1;
        private VisualElement dragParent;
        private Vector2 dragStartPointer;
        private Vector2 dragStartPosition;

        public WindowState State { get; private set; } =
            WindowState.Normal;

        public bool IsFocused { get; private set; }

        public bool IsMinimized =>
            State == WindowState.Minimized;

        public event Action Closed;
        public event Action StateChanged;

        private bool CanOperate =>
            initialized && !disposed && !closeRequested;

        // DesktopController supplies the specific cloned app tree.
        public void Initialize(
            VisualElement root,
            bool allowResize)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(WindowController));

            if (initialized)
            {
                throw new InvalidOperationException(
                    "This window has already been initialized.");
            }

            if (root == null)
                throw new ArgumentNullException(nameof(root));

            var newBinder = new WindowBinder(root);

            if (!newBinder.IsValid ||
                newBinder.Root == null ||
                newBinder.TitleBar == null ||
                newBinder.MinimizeButton == null ||
                newBinder.MaximizeButton == null ||
                newBinder.CloseButton == null)
            {
                throw new InvalidOperationException(
                    "The app visual tree is missing required window elements.");
            }

            instanceRoot = root;
            binder = newBinder;
            view = new WindowView(binder);
            isResizable = allowResize;

            State = WindowState.Normal;
            stateBeforeMinimize = WindowState.Normal;
            IsFocused = false;

            // The shell fills the instance positioned by AppController.
            binder.Root.style.width = Length.Percent(100);
            binder.Root.style.height = Length.Percent(100);

            binder.MaximizeButton.SetEnabled(isResizable);

            view.SetFocused(false);
            view.SetMaximized(false);
            view.Show();
            instanceRoot.style.display = DisplayStyle.Flex;

            initialized = true;
            RegisterCallbacks();

            // AppController activates after registering its event handlers.
        }

        private void RegisterCallbacks()
        {
            binder.Root.RegisterCallback<PointerDownEvent>(
                OnWindowPointerDown,
                TrickleDown.TrickleDown);

            binder.TitleBar.RegisterCallback<PointerDownEvent>(
                OnHeaderPointerDown,
                TrickleDown.TrickleDown);

            binder.TitleBar.RegisterCallback<PointerMoveEvent>(
                OnHeaderPointerMove);

            binder.TitleBar.RegisterCallback<PointerUpEvent>(
                OnHeaderPointerUp);

            binder.TitleBar.RegisterCallback<PointerCancelEvent>(
                OnHeaderPointerCancel);

            binder.TitleBar.RegisterCallback<PointerCaptureOutEvent>(
                OnHeaderPointerCaptureOut);

            instanceRoot.RegisterCallback<DetachFromPanelEvent>(
                OnDetachedFromPanel);

            binder.MinimizeButton.clicked += OnMinimizePressed;
            binder.MaximizeButton.clicked += OnMaximizePressed;
            binder.CloseButton.clicked += OnClosePressed;
        }

        private void UnregisterCallbacks()
        {
            if (binder == null)
                return;

            binder.Root.UnregisterCallback<PointerDownEvent>(
                OnWindowPointerDown,
                TrickleDown.TrickleDown);

            binder.TitleBar.UnregisterCallback<PointerDownEvent>(
                OnHeaderPointerDown,
                TrickleDown.TrickleDown);

            binder.TitleBar.UnregisterCallback<PointerMoveEvent>(
                OnHeaderPointerMove);

            binder.TitleBar.UnregisterCallback<PointerUpEvent>(
                OnHeaderPointerUp);

            binder.TitleBar.UnregisterCallback<PointerCancelEvent>(
                OnHeaderPointerCancel);

            binder.TitleBar.UnregisterCallback<PointerCaptureOutEvent>(
                OnHeaderPointerCaptureOut);

            instanceRoot.UnregisterCallback<DetachFromPanelEvent>(
                OnDetachedFromPanel);

            binder.MinimizeButton.clicked -= OnMinimizePressed;
            binder.MaximizeButton.clicked -= OnMaximizePressed;
            binder.CloseButton.clicked -= OnClosePressed;
        }

        private void OnWindowPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0)
                Focus();
        }

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (!CanOperate ||
                !isActiveAndEnabled ||
                State != WindowState.Normal ||
                isDragging ||
                evt.button != 0 ||
                evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
            {
                return;
            }

            if (IsInteractiveHeaderTarget(evt.target as VisualElement))
                return;

            var parent = instanceRoot.parent;
            if (parent == null)
                return;

            Focus();

            // Focus invokes external handlers, so check again.
            if (!CanOperate ||
                State != WindowState.Normal ||
                instanceRoot.parent != parent)
            {
                return;
            }

            dragParent = parent;
            dragPointerId = evt.pointerId;

            // Parent coordinates account for UI scaling.
            dragStartPointer = dragParent.WorldToLocal(evt.position);

            dragStartPosition = new Vector2(
                instanceRoot.resolvedStyle.left,
                instanceRoot.resolvedStyle.top);

            isDragging = true;
            binder.TitleBar.CapturePointer(dragPointerId);

            evt.StopPropagation();
        }

        private bool IsInteractiveHeaderTarget(VisualElement target)
        {
            // Exclude controls and their children, including button icons.
            for (var element = target;
                 element != null && element != binder.TitleBar;
                 element = element.parent)
            {
                if (element is Button ||
                    element.focusable ||
                    element.name == "WindowControls" ||
                    element.ClassListContains("browser-tab"))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnHeaderPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging || evt.pointerId != dragPointerId)
                return;

            if (!CanOperate ||
                State != WindowState.Normal ||
                instanceRoot.parent != dragParent ||
                !binder.TitleBar.HasPointerCapture(dragPointerId) ||
                (evt.pressedButtons & 1) == 0)
            {
                EndDrag();
                return;
            }

            Vector2 pointer = dragParent.WorldToLocal(evt.position);

            Vector2 position =
                dragStartPosition + (pointer - dragStartPointer);

            instanceRoot.style.left = position.x;
            instanceRoot.style.top = position.y;

            evt.StopPropagation();
        }

        private void OnHeaderPointerUp(PointerUpEvent evt)
        {
            if (!isDragging ||
                evt.pointerId != dragPointerId ||
                evt.button != 0)
            {
                return;
            }

            EndDrag();
            evt.StopPropagation();
        }

        private void OnHeaderPointerCancel(PointerCancelEvent evt)
        {
            if (isDragging && evt.pointerId == dragPointerId)
                EndDrag();
        }

        private void OnHeaderPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (isDragging && evt.pointerId == dragPointerId)
                EndDrag();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.target == instanceRoot)
                EndDrag();
        }

        private void EndDrag()
        {
            if (!isDragging)
                return;

            int pointerId = dragPointerId;

            // Clear first because releasing capture can invoke a callback.
            isDragging = false;
            dragPointerId = -1;
            dragParent = null;

            if (binder?.TitleBar != null &&
                binder.TitleBar.HasPointerCapture(pointerId))
            {
                binder.TitleBar.ReleasePointer(pointerId);
            }
        }

        private void LateUpdate()
        {
            if (!isDragging)
                return;

            if (!CanOperate ||
                State != WindowState.Normal ||
                instanceRoot.parent != dragParent ||
                !binder.TitleBar.HasPointerCapture(dragPointerId))
            {
                EndDrag();
            }
        }

        private void OnDisable()
        {
            EndDrag();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                EndDrag();
        }

        public void Activate()
        {
            if (!CanOperate)
                return;

            if (IsMinimized)
            {
                RestoreFromMinimized();
                return;
            }

            instanceRoot.style.display = DisplayStyle.Flex;
            view.Show();
            Focus();
        }

        public void Focus()
        {
            if (!CanOperate || IsMinimized)
                return;

            instanceRoot.BringToFront();

            if (focusedWindow == this && IsFocused)
                return;

            if (focusedWindow != null && focusedWindow != this)
                focusedWindow.Unfocus();

            focusedWindow = this;
            IsFocused = true;

            view.SetFocused(true);
            StateChanged?.Invoke();
        }

        public void Unfocus()
        {
            if (!CanOperate || !IsFocused)
                return;

            EndDrag();

            if (focusedWindow == this)
                focusedWindow = null;

            IsFocused = false;
            view.SetFocused(false);

            StateChanged?.Invoke();
        }

        private void OnMinimizePressed()
        {
            Minimize();
        }

        public void Minimize()
        {
            if (!CanOperate || IsMinimized)
                return;

            EndDrag();

            // Preserve maximized state through minimize/restore.
            stateBeforeMinimize = State;
            State = WindowState.Minimized;

            if (focusedWindow == this)
                focusedWindow = null;

            IsFocused = false;

            view.SetFocused(false);
            view.Hide();
            instanceRoot.style.display = DisplayStyle.None;

            StateChanged?.Invoke();
        }

        public void RestoreFromMinimized()
        {
            if (!CanOperate || !IsMinimized)
                return;

            State = stateBeforeMinimize;

            instanceRoot.style.display = DisplayStyle.Flex;
            view.Show();
            view.SetMaximized(State == WindowState.Maximized);

            // Focus also publishes the restored state.
            Focus();
        }

        private void OnMaximizePressed()
        {
            ToggleMaximize();
        }

        public void ToggleMaximize()
        {
            if (!CanOperate || !isResizable || IsMinimized)
                return;

            if (State == WindowState.Maximized)
                Restore();
            else
                Maximize();
        }

        public void Maximize()
        {
            if (!CanOperate ||
                !isResizable ||
                IsMinimized ||
                State == WindowState.Maximized)
            {
                return;
            }

            EndDrag();
            SaveCurrentBounds();

            State = WindowState.Maximized;

            instanceRoot.style.left = 0;
            instanceRoot.style.top = 0;
            instanceRoot.style.width = Length.Percent(100);
            instanceRoot.style.height = Length.Percent(100);

            // Allow the window to fit a desktop smaller than its minimum.
            instanceRoot.style.minWidth = 0;
            instanceRoot.style.minHeight = 0;

            view.SetMaximized(true);

            if (IsFocused)
                StateChanged?.Invoke();
            else
                Focus();
        }

        public void Restore()
        {
            if (!CanOperate || State != WindowState.Maximized)
                return;

            EndDrag();

            State = WindowState.Normal;

            instanceRoot.style.left = restoredLeft;
            instanceRoot.style.top = restoredTop;
            instanceRoot.style.width = restoredWidth;
            instanceRoot.style.height = restoredHeight;
            instanceRoot.style.minWidth = restoredMinWidth;
            instanceRoot.style.minHeight = restoredMinHeight;

            view.SetMaximized(false);

            if (IsFocused)
                StateChanged?.Invoke();
            else
                Focus();
        }

        private void SaveCurrentBounds()
        {
            // Preserve inline styles, including the position after dragging.
            restoredLeft = instanceRoot.style.left;
            restoredTop = instanceRoot.style.top;
            restoredWidth = instanceRoot.style.width;
            restoredHeight = instanceRoot.style.height;
            restoredMinWidth = instanceRoot.style.minWidth;
            restoredMinHeight = instanceRoot.style.minHeight;
        }

        private void OnClosePressed()
        {
            Close();
        }

        public void Close()
        {
            if (!CanOperate)
                return;

            EndDrag();
            closeRequested = true;

            if (focusedWindow == this)
                focusedWindow = null;

            IsFocused = false;
            view.SetFocused(false);
            view.Hide();
            instanceRoot.style.display = DisplayStyle.None;

            try
            {
                // AppController removes the app and its runtime content.
                Closed?.Invoke();
            }
            finally
            {
                // Also clean up if no AppController subscribed.
                Dispose();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            EndDrag();

            disposed = true;
            UnregisterCallbacks();

            if (focusedWindow == this)
                focusedWindow = null;

            IsFocused = false;
            initialized = false;

            instanceRoot?.RemoveFromHierarchy();

            instanceRoot = null;
            binder = null;
            view = null;

            Closed = null;
            StateChanged = null;

            // Dedicated GameObject created by the window factory.
            if (!destroying)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            destroying = true;

            if (disposed)
                return;

            EndDrag();

            if (initialized && !closeRequested)
            {
                // Notify AppController if destroyed externally.
                closeRequested = true;

                if (focusedWindow == this)
                    focusedWindow = null;

                IsFocused = false;

                try
                {
                    Closed?.Invoke();
                }
                finally
                {
                    Dispose();
                }
            }
            else
            {
                Dispose();
            }
        }
    }
}