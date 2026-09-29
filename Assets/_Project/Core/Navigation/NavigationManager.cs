using System;
using UnityEngine;

namespace Atlas.Core.Navigation
{
    public enum NavigationAction
    {
        Back,
        Home,
        Close,
        Cancel
    }

    public sealed class NavigationManager : MonoBehaviour
    {
        public static NavigationManager Instance { get; private set; }
        public event Action<NavigationAction> NavigationRequested;

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

        public void Request(NavigationAction action)
        {
            NavigationRequested?.Invoke(action);
        }

        public void Back()
        {
            Request(NavigationAction.Back);
        }
    }
}