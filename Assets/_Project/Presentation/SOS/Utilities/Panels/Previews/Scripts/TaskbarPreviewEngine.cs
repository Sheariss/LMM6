using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPreviewEngine
    {
        private readonly AppController apps;
        private readonly WindowPreviewCaptureService captures;
        private readonly Func<bool> canUse;

        public bool CanUse => canUse();

        public TaskbarPreviewEngine(
            AppController apps,
            WindowPreviewCaptureService captures,
            Func<bool> canUse)
        {
            this.apps = apps != null
                ? apps
                : throw new ArgumentNullException(nameof(apps));

            this.captures = captures != null
                ? captures
                : throw new ArgumentNullException(nameof(captures));

            this.canUse = canUse ??
                throw new ArgumentNullException(nameof(canUse));
        }

        public IReadOnlyList<AppWindowPreview> GetWindows(string appID)
        {
            if (!CanUse)
                return Array.Empty<AppWindowPreview>();

            return apps.GetWindowPreviews(appID);
        }

        public Texture GetThumbnail(IAppWindow window)
        {
            return captures.GetThumbnail(window);
        }

        public bool Activate(string appID, IAppWindow window)
        {
            return CanUse &&
                apps.TryActivateWindow(appID, window);
        }

        public bool Close(string appID, IAppWindow window)
        {
            return CanUse &&
                apps.TryCloseWindow(appID, window);
        }
    }
}