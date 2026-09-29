using System;
using UnityEngine;

namespace Atlas.Core
{
    public enum ThemeType
    {
        Default,
        Light,
        Dark,
        HighContrast
    }

    public sealed class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }
        public ThemeType CurrentTheme { get; private set; } = ThemeType.Default;
        public event Action<ThemeType> ThemeChanged;

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

        public void SetTheme(ThemeType theme)
        {
            if (CurrentTheme == theme)
                return;

            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
    }
}