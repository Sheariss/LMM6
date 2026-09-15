using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Atlas.Core.Persistence
{
    public sealed class SaveService
    {
        // -------------------- FILE CONFIGURATION --------------------
        private const string SaveDirectoryName = "Saves";
        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExtension = ".json";

        private readonly string saveDirectoryPath;

        // -------------------- INITIALIZATION --------------------
        public SaveService()
            : this(Application.persistentDataPath)
        {
        }

        public SaveService(string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new ArgumentException(
                    "Persistent data path cannot be null or empty.",
                    nameof(persistentDataPath)
                );
            }

            saveDirectoryPath = Path.Combine(
                persistentDataPath,
                SaveDirectoryName
            );
        }

        // -------------------- PATH & FILE HELPERS --------------------
        public string ResolveSavePath(int slot)
        {
            ValidateSlot(slot);

            string fileName =
                $"{SaveFilePrefix}{slot}{SaveFileExtension}";

            return Path.Combine(
                saveDirectoryPath,
                fileName
            );
        }

        public bool SaveExists(int slot)
        {
            ValidateSlot(slot);

            string path = ResolveSavePath(slot);

            return File.Exists(path);
        }
        
        // -------------------- PRIVATE VALIDATION --------------------
        private static void ValidateSlot(int slot)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slot),
                    slot,
                    "Save slot cannot be negative."
                );
            }
        }

        // -------------------- PUBLIC OPERATIONS --------------------
        public bool Save(int slot, GameSaveData data)
        {
            ValidateSlot(slot);

            if (data is null)
            {
                Debug.LogError(
                    $"[{nameof(SaveService)}] Cannot save null data."
                );

                return false;
            }

            try
            {
                Directory.CreateDirectory(saveDirectoryPath);

                string json = SaveSerializer.ToJson(data);
                string path = ResolveSavePath(slot);

                File.WriteAllText(
                    path,
                    json,
                    new UTF8Encoding(false)
                );

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SaveService)}] Failed to save slot {slot}.\n" +
                    exception
                );

                return false;
            }
        }

        public bool TryLoad(int slot, out GameSaveData data)
        {
            ValidateSlot(slot);

            data = null;

            string path = ResolveSavePath(slot);

            if (!File.Exists(path))
            {
                Debug.LogWarning(
                    $"[{nameof(SaveService)}] " +
                    $"Save slot {slot} does not exist."
                );

                return false;
            }

            try
            {
                string json = File.ReadAllText(path);

                GameSaveData loadedData =
                    SaveSerializer.FromJson<GameSaveData>(json);
                
                data = loadedData;

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SaveService)}] Failed to load slot {slot}.\n" +
                    exception
                );

                return false;
            }
        }

        public bool DeleteSave(int slot)
        {
            ValidateSlot(slot);

            string path = ResolveSavePath(slot);

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                File.Delete(path);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{nameof(SaveService)}] Failed to delete slot {slot}.\n" +
                    exception
                );

                return false;
            }
        }
    }    
}