using System;
using UnityEngine;

namespace Atlas.Core.Shortcut
{
    public enum GlobalShortcut
    {
        Back,
        Save,
        SecurityScreen,
        Help,
        ToggleFullscreen
    }

    public sealed class ShortcutManager : MonoBehaviour
    {
        public static ShortcutManager Instance { get; private set; }
        public event Action<GlobalShortcut> ShortcutTriggered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Trigger(GlobalShortcut shortcut)
        {
            ShortcutTriggered?.Invoke(shortcut);
        }
    }
}