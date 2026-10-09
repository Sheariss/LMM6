using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.Apps
{
    [Serializable]
    public sealed class AppDefinition
    {
        [Header("Identity")]
        [SerializeField] private string appID;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite appIcon48;
        [SerializeField] private Sprite appIcon96;

        [Header("Window")]
        [SerializeField] private VisualTreeAsset appUXML;
        [SerializeField] private bool isResizable = true;
        [SerializeField] private bool allowMultipleInstances;

        [SerializeField]
        private Vector2 defaultWindowSize =
            new Vector2(800f, 600f);

        [SerializeField]
        private Vector2 minimumWindowSize =
            new Vector2(320f, 240f);

        [Header("Default Placement")]
        [SerializeField] private bool showDesktopShortcut = true;
        [SerializeField] private bool pinToTaskbar;

        public string AppID => appID;
        public string DisplayName => displayName;
        public Sprite AppIcon48 => appIcon48;
        public Sprite AppIcon96 => appIcon96;

        public VisualTreeAsset AppUXML => appUXML;
        public bool IsResizable => isResizable;
        public bool AllowMultipleInstances => allowMultipleInstances;
        public Vector2 DefaultWindowSize => defaultWindowSize;
        public Vector2 MinimumWindowSize => minimumWindowSize;

        public bool ShowDesktopShortcut => showDesktopShortcut;
        public bool PinToTaskbar => pinToTaskbar;
    }

    [CreateAssetMenu(
        fileName = "App Catalog",
        menuName = "ATLAS/App Catalog")]
    public sealed class AppCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AppDefinition> apps = new List<AppDefinition>();

        public IReadOnlyList<AppDefinition> Apps => apps;

        public bool TryGetApp(string appID, out AppDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(appID))
                return false;

            foreach (AppDefinition app in apps)
            {
                if (app != null && app.AppID == appID)
                {
                    definition = app;
                    return true;
                }
            }

            return false;
        }
    }
}