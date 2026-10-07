using Atlas.Presentation.Settings;
using System;
using UnityEngine;

namespace Atlas.Core.Settings
{
    public sealed class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [SerializeField] private SettingsCatalog catalog;

        private SettingsEngine engine;
        private SettingsSaveData current;

        public SettingsCatalog Catalog => catalog;

        public SettingsEngine Engine
        {
            get
            {
                Initialize();
                return engine;
            }
        }

        // Return a copy so callers cannot mutate committed settings.
        public SettingsSaveData Current
        {
            get
            {
                Initialize();
                return current.Clone();
            }
        }

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

            SettingsEngine newEngine = new(catalog);
            SettingsSaveData defaults = newEngine.CreateDefaults();

            engine = newEngine;
            current = defaults;
        }

        public void Apply(SettingsSaveData proposed)
        {
            if (proposed == null)
                throw new ArgumentNullException(nameof(proposed));

            Initialize();

            SettingsSaveData validated = engine.Normalize(proposed);

            if (engine.AreEqual(current, validated))
                return;

            current = validated;
            SettingsChanged?.Invoke();
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