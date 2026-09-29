using System;
using UnityEngine;

namespace Atlas.Core.Inpput
{
    public enum InputMode
    {
        Normal,
        UIOnly,
        Disabled
    }

    public sealed class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public InputMode CurrentMode { get; private set; } = InputMode.Normal;
        public event Action<InputMode> InputModeChanged;

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

        public void SetInputMode(InputMode mode)
        {
            if (CurrentMode == mode)
                return;

            CurrentMode = mode;
            InputModeChanged?.Invoke(mode);
        }

        public bool AllowsInput()
        {
            return CurrentMode != InputMode.Disabled;
        }
    }
}