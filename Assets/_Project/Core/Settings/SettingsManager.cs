using System;
using UnityEngine;

namespace Atlas.Core.Settings
{
    [Serializable]
    public sealed class SettingsData
    {
        public bool Fullscreen = true;
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SFXVolume = 1f;
        public float NotificationVolume = 1f;
    }

    public sealed class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }
        public SettingsData Current { get; private set; } = new();
        public event Action SettingsChanged;

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

        public void Apply(SettingsData settings)
        {
            if (settings == null)
                return;

            Current = settings;
            SettingsChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            Current = new SettingsData();
            SettingsChanged?.Invoke();
        }
    }
}