using UnityEngine;
using Atlas.Core.Persistence.Data;

namespace Atlas.Core.Persistence
{
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        public GameSaveData CurrentSave { get; private set; }

        public int CurrentSaveSlot { get; private set; } = -1;

        public bool HasSelectedSaveSlot =>
            CurrentSaveSlot >= 0;

        public bool HasActiveSave =>
            CurrentSave != null;

        private SaveService saveService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(SaveManager)}] Duplicate instance detected. " +
                    "Destroying duplicate."
                );

                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Initialize()
        {
            saveService = new SaveService();

            IsInitialized = true;
        }

        public bool SelectSaveSlot(int slot)
        {
            if (slot < 0)
            {
                Debug.LogWarning(
                    $"[{nameof(SaveManager)}] " +
                    $"Invalid save slot requested: {slot}."
                );

                return false;
            }

            CurrentSaveSlot = slot;

            return true;
        }

        public bool HasSave(int slot)
        {
            if (!IsInitialized || slot < 0)
            {
                return false;
            }

            return saveService.SaveExists(slot);
        }

        public bool NewGame()
        {
            if (!IsInitialized)
            {
                return false;
            }

            if (!HasSelectedSaveSlot)
            {
                Debug.LogWarning(
                    $"[{nameof(SaveManager)}] " +
                    "Cannot create a new game without a selected save slot."
                );

                return false;
            }

            CurrentSave = CreateDefaultSaveData();

            return true;
        }

        public bool SaveGame()
        {
            if (!CanPersistCurrentSave())
            {
                return false;
            }

            // TODO: Runtime domain state collection will be added here.
            //
            // Example later:
            //
            // CurrentSave.investigation =
            //     InvestigationManager.Instance.ExportSaveData();
            //
            // CurrentSave.progression =
            //     ProgressionManager.Instance.ExportSaveData();

            bool success = saveService.Save(
                CurrentSaveSlot,
                CurrentSave
            );
            return success;
        }

        public bool LoadGame()
        {
            if (!IsInitialized || !HasSelectedSaveSlot)
            {
                return false;
            }

            if (!saveService.TryLoad(
                    CurrentSaveSlot,
                    out GameSaveData loadedSave))
            {
                return false;
            }

            if (!ValidateLoadedSave(loadedSave))
            {
                Debug.LogError(
                    $"[{nameof(SaveManager)}] " +
                    $"Save slot {CurrentSaveSlot} failed validation."
                );

                return false;
            }

            // IMPORTANT:
            //
            // CurrentSave is only replaced after the file has been
            // successfully read, deserialized, and validated.
            CurrentSave = loadedSave;

            // Runtime restoration will be coordinated here later.

            return true;
        }

        public bool DeleteSave()
        {
            if (!IsInitialized || !HasSelectedSaveSlot)
            {
                return false;
            }

            bool success =
                saveService.DeleteSave(CurrentSaveSlot);

            if (!success)
            {
                return false;
            }

            CurrentSave = null;

            return true;
        }

        private bool CanPersistCurrentSave()
        {
            if (!IsInitialized)
            {
                return false;
            }

            if (!HasSelectedSaveSlot)
            {
                Debug.LogWarning(
                    $"[{nameof(SaveManager)}] " +
                    "No save slot is currently selected."
                );

                return false;
            }

            if (!HasActiveSave)
            {
                Debug.LogWarning(
                    $"[{nameof(SaveManager)}] " +
                    "No active save data exists."
                );

                return false;
            }

            return true;
        }

        private static GameSaveData CreateDefaultSaveData()
        {
            return new GameSaveData();
        }

        private static bool ValidateLoadedSave(GameSaveData data)
        {
            if (data is null)
            {
                return false;
            }
            // TODO: Save-version, required-domain, identifier, and enum
            // validation will be added as the data model is implemented.
            return true;
        }

        public bool HasAnySave()
        {
            if (!IsInitialized)
            {
                return false;
            }

            return saveService.HasAnySave();
        }
    }
}