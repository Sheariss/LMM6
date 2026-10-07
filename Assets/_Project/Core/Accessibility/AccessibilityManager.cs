using System;
using UnityEngine;

namespace Atlas.Core.Accessibility
{
    public sealed class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [SerializeField] private AccessibilityCatalog catalog;

        private AccessibilityEngine engine;
        private AccessibilitySaveData current;

        public AccessibilityCatalog Catalog => catalog;

        public AccessibilityEngine Engine
        {
            get
            {
                Initialize();
                return engine;
            }
        }

        public AccessibilitySaveData Current
        {
            get
            {
                Initialize();
                return current.Clone();
            }
        }

        public event Action AccessibilityChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        public void Initialize()
        {
            if (engine != null)
                return;

            AccessibilityEngine candidate = new(catalog);
            AccessibilitySaveData defaults = candidate.CreateDefaults();

            engine = candidate;
            current = defaults;
        }

        public void Apply(AccessibilitySaveData proposed)
        {
            if (proposed == null)
                throw new ArgumentNullException(nameof(proposed));

            Initialize();

            AccessibilitySaveData normalized = engine.Normalize(proposed);

            if (engine.AreEqual(current, normalized))
                return;

            current = normalized;
            AccessibilityChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            Apply(Engine.CreateDefaults());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}