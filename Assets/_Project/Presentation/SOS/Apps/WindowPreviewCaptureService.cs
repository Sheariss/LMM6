using System;
using System.Collections;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class WindowPreviewCaptureService : MonoBehaviour
    {
        [Header("Thumbnail Resolution")]
        [SerializeField, Min(64)] private int maximumWidth = 320;
        [SerializeField, Min(64)] private int maximumHeight = 180;

        [Header("Refresh")]
        [SerializeField, Min(0.1f)] private float captureInterval = 0.2f;
        [SerializeField, Min(0.2f)] private float idleRefreshInterval = 1f;

        [Header("Platform Calibration")]
        [Tooltip("Enable only if captured images are vertically inverted.")]
        [SerializeField] private bool flipCaptureVertically;

        private sealed class Entry
        {
            public RenderTexture Texture;
            public Rect Bounds;
            public uint Revision;
            public float CapturedAt;
        }

        private readonly Dictionary<IAppWindow, Entry> entries = new();

        private AppController appController;
        private Func<bool> canCapture;
        private Coroutine routine;
        private RenderTexture screenTexture;
        private float nextCaptureTime;

        public void Initialize(
            AppController appController,
            Func<bool> canCapture)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "WindowPreviewCaptureService must be active.");
            }

            this.appController = appController != null
                ? appController
                : throw new ArgumentNullException(nameof(appController));

            this.canCapture = canCapture ??
                throw new ArgumentNullException(nameof(canCapture));

            appController.AppStateChanged += OnAppStateChanged;

            nextCaptureTime = 0f;
            routine = StartCoroutine(CaptureLoop());
        }

        public Texture GetThumbnail(IAppWindow window)
        {
            if (window != null &&
                entries.TryGetValue(window, out Entry entry) &&
                entry.Texture != null &&
                entry.Texture.IsCreated())
            {
                return entry.Texture;
            }

            return null;
        }

        private IEnumerator CaptureLoop()
        {
            var endOfFrame = new WaitForEndOfFrame();

            while (true)
            {
                yield return endOfFrame;

                if (Time.unscaledTime < nextCaptureTime)
                    continue;

                nextCaptureTime =
                    Time.unscaledTime + Mathf.Max(0.1f, captureInterval);

                bool failed = false;

                try
                {
                    CaptureIfNeeded();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    failed = true;
                }

                if (failed)
                    break;
            }

            routine = null;
        }

        private void CaptureIfNeeded()
        {
            if (appController == null || canCapture == null)
                return;

            IReadOnlyList<AppWindowPreview> windows =
                appController.GetAllWindowPreviews();

            Prune(windows);

            if (!canCapture() || !Application.isFocused)
                return;

            AppWindowPreview target = null;

            foreach (AppWindowPreview window in windows)
            {
                if (window.IsFocused && IsVisible(window))
                {
                    target = window;
                    break;
                }
            }

            if (target == null ||
                !TryGetCaptureRect(
                    target,
                    windows,
                    out Rect bounds,
                    out Rect panelBounds))
            {
                return;
            }

            entries.TryGetValue(target.Window, out Entry entry);

            bool invalidated =
                entry == null ||
                entry.Texture == null ||
                !entry.Texture.IsCreated() ||
                entry.Revision != target.Revision ||
                entry.Bounds != bounds;

            bool refreshDue =
                entry == null ||
                Time.unscaledTime - entry.CapturedAt >=
                    Mathf.Max(captureInterval, idleRefreshInterval);

            if (!invalidated && !refreshDue)
                return;

            Capture(target, bounds, panelBounds, entry);
        }

        private static bool IsVisible(AppWindowPreview window)
        {
            return !window.IsMinimized &&
                window.Root != null &&
                window.Root.panel != null &&
                window.Root.resolvedStyle.display != DisplayStyle.None &&
                window.Root.resolvedStyle.visibility != Visibility.Hidden;
        }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            return inner.width > 0f &&
                inner.height > 0f &&
                inner.xMin >= outer.xMin &&
                inner.yMin >= outer.yMin &&
                inner.xMax <= outer.xMax &&
                inner.yMax <= outer.yMax;
        }

        private static bool TryGetCaptureRect(
            AppWindowPreview target,
            IReadOnlyList<AppWindowPreview> windows,
            out Rect bounds,
            out Rect panelBounds)
        {
            bounds = target.Root.worldBound;
            panelBounds = target.Root.panel.visualTree.worldBound;

            VisualElement windowLayer = target.Root.parent;

            if (windowLayer == null ||
                !ContainsRect(windowLayer.worldBound, bounds) ||
                !ContainsRect(panelBounds, bounds) ||
                float.IsNaN(bounds.width) ||
                float.IsNaN(bounds.height) ||
                float.IsInfinity(bounds.width) ||
                float.IsInfinity(bounds.height))
            {
                return false;
            }

            int targetIndex = windowLayer.IndexOf(target.Root);

            foreach (AppWindowPreview other in windows)
            {
                if (ReferenceEquals(other.Window, target.Window) ||
                    !IsVisible(other) ||
                    other.Root.parent != windowLayer)
                {
                    continue;
                }

                if (windowLayer.IndexOf(other.Root) > targetIndex &&
                    other.Root.worldBound.Overlaps(bounds))
                {
                    return false;
                }
            }

            return true;
        }

        private void Capture(
            AppWindowPreview target,
            Rect bounds,
            Rect panelBounds,
            Entry entry)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            if (screenTexture == null ||
                screenTexture.width != Screen.width ||
                screenTexture.height != Screen.height ||
                !screenTexture.IsCreated())
            {
                ReleaseTexture(screenTexture);

                screenTexture = CreateTexture(
                    Screen.width,
                    Screen.height,
                    "Desktop preview capture");
            }

            if (entry == null)
            {
                entry = new Entry();
                entries.Add(target.Window, entry);
            }

            float scale = Mathf.Min(
                Mathf.Max(64, maximumWidth) / bounds.width,
                Mathf.Max(64, maximumHeight) / bounds.height);

            int width = Mathf.Max(
                1,
                Mathf.RoundToInt(bounds.width * scale));

            int height = Mathf.Max(
                1,
                Mathf.RoundToInt(bounds.height * scale));

            if (entry.Texture == null ||
                entry.Texture.width != width ||
                entry.Texture.height != height ||
                !entry.Texture.IsCreated())
            {
                ReleaseTexture(entry.Texture);

                entry.Texture = CreateTexture(
                    width,
                    height,
                    "Window preview");
            }

            var uvScale = new Vector2(
                bounds.width / panelBounds.width,
                bounds.height / panelBounds.height);

            var uvOffset = new Vector2(
                (bounds.xMin - panelBounds.xMin) / panelBounds.width,
                1f - (bounds.yMax - panelBounds.yMin) / panelBounds.height);

            if (flipCaptureVertically)
            {
                uvOffset.y = 1f - uvOffset.y;
                uvScale.y = -uvScale.y;
            }

            RenderTexture previousTarget = RenderTexture.active;
            bool previousSRGB = GL.sRGBWrite;

            try
            {
                ScreenCapture.CaptureScreenshotIntoRenderTexture(
                    screenTexture);

                GL.sRGBWrite =
                    QualitySettings.activeColorSpace == ColorSpace.Linear;

                Graphics.Blit(
                    screenTexture,
                    entry.Texture,
                    uvScale,
                    uvOffset);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                GL.sRGBWrite = previousSRGB;
            }

            entry.Bounds = bounds;
            entry.Revision = target.Revision;
            entry.CapturedAt = Time.unscaledTime;
        }

        private static RenderTexture CreateTexture(
            int width,
            int height,
            string textureName)
        {
            var texture = new RenderTexture(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = textureName,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!texture.Create())
            {
                Destroy(texture);

                throw new InvalidOperationException(
                    "Could not create a preview render texture.");
            }

            return texture;
        }

        private void OnAppStateChanged(
            string appID,
            int count,
            bool focused)
        {
            if (appController != null)
                Prune(appController.GetAllWindowPreviews());
        }

        private void Prune(IReadOnlyList<AppWindowPreview> windows)
        {
            var live = new HashSet<IAppWindow>();

            foreach (AppWindowPreview window in windows)
                live.Add(window.Window);

            var removed = new List<IAppWindow>();

            foreach (var pair in entries)
            {
                if (!live.Contains(pair.Key))
                    removed.Add(pair.Key);
            }

            foreach (IAppWindow window in removed)
            {
                ReleaseTexture(entries[window].Texture);
                entries.Remove(window);
            }
        }

        private static void ReleaseTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            Destroy(texture);
        }

        public void Shutdown()
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = null;

            if (appController != null)
                appController.AppStateChanged -= OnAppStateChanged;

            foreach (Entry entry in entries.Values)
                ReleaseTexture(entry.Texture);

            entries.Clear();

            ReleaseTexture(screenTexture);
            screenTexture = null;

            appController = null;
            canCapture = null;
        }

        private void OnDisable()
        {
            Shutdown();
        }
    }
}