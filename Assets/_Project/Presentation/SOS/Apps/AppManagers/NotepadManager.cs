using System;
using System.Collections.Generic;
using Atlas.Core.Persistence;
using UnityEngine;

namespace Atlas.SOS.Notepad
{
    public sealed class NotepadManager : MonoBehaviour
    {
        public static NotepadManager Instance { get; private set; }

        private readonly Dictionary<
            string,
            Dictionary<string, NotepadDocument>> documents =
                new(StringComparer.Ordinal);

        private readonly Dictionary<string, string> lastDocumentIds =
            new(StringComparer.Ordinal);

        private object saveReference;
        private int saveSlot;
        private int epoch;
        private bool scopeInitialized;

        public int Epoch
        {
            get
            {
                SynchronizeScope();
                return epoch;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SynchronizeScope();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ClearSession()
        {
            documents.Clear();
            lastDocumentIds.Clear();
            epoch++;
        }

        public void Store(string userId, NotepadDocument document)
        {
            SynchronizeScope();

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("A user ID is required.");

            if (document == null || string.IsNullOrWhiteSpace(document.Id))
                throw new ArgumentException("A valid document is required.");

            if (!documents.TryGetValue(userId, out var userDocuments))
            {
                userDocuments =
                    new Dictionary<string, NotepadDocument>(
                        StringComparer.Ordinal);

                documents.Add(userId, userDocuments);
            }

            userDocuments[document.Id] = document.Clone();
            lastDocumentIds[userId] = document.Id;
        }

        public bool TryGet(
            string userId,
            string documentId,
            out NotepadDocument document)
        {
            SynchronizeScope();
            document = null;

            if (!documents.TryGetValue(userId, out var userDocuments) ||
                !userDocuments.TryGetValue(documentId, out var stored))
            {
                return false;
            }

            document = stored.Clone();
            return true;
        }

        public bool TryGetLast(
            string userId,
            out NotepadDocument document)
        {
            SynchronizeScope();
            document = null;

            return lastDocumentIds.TryGetValue(userId, out string id) &&
                   TryGet(userId, id, out document);
        }

        public List<NotepadDocument> GetDocuments(string userId)
        {
            SynchronizeScope();

            List<NotepadDocument> result = new();

            if (documents.TryGetValue(userId, out var userDocuments))
            {
                foreach (NotepadDocument document in userDocuments.Values)
                    result.Add(document.Clone());
            }

            result.Sort((left, right) =>
            {
                int comparison = string.Compare(
                    left.Name,
                    right.Name,
                    StringComparison.OrdinalIgnoreCase);

                return comparison != 0
                    ? comparison
                    : string.CompareOrdinal(left.Id, right.Id);
            });

            return result;
        }

        private void SynchronizeScope()
        {
            SaveManager saves = SaveManager.Instance;
            object currentSave = saves != null ? saves.CurrentSave : null;
            int currentSlot = saves != null ? saves.CurrentSaveSlot : -1;

            if (scopeInitialized &&
                ReferenceEquals(saveReference, currentSave) &&
                saveSlot == currentSlot)
            {
                return;
            }

            saveReference = currentSave;
            saveSlot = currentSlot;
            scopeInitialized = true;

            ClearSession();
        }
    }
}