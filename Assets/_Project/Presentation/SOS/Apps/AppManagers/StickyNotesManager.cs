using System;
using System.Collections.Generic;
using Atlas.Core.Persistence;
using Atlas.Core.Persistence.Data;
using Atlas.SOS.Authentication;
using UnityEngine;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesManager : MonoBehaviour
    {
        public static StickyNotesManager Instance { get; private set; }

        [SerializeField]
        private StickyNotesLibrary library;

        public event Action Changed;

        private GameSaveData boundSave;
        private string boundUserId;
        private bool contextKnown;
        private long generation;

        private StickyNotesSaveData transientData = new();
        private StickyNotesEngine engine;
        private StickyNotesSession session;

        private IStickyNotesWidgetHost widgetHost;

        public StickyNotesSession Session
        {
            get
            {
                RefreshContext();
                return session;
            }
        }

        private bool HasWidgetHost =>
            widgetHost != null &&
            (!(widgetHost is UnityEngine.Object unityObject) ||
                unityObject != null);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (library == null)
            {
                Debug.LogError(
                    "[StickyNotes] Assign a StickyNotesLibrary.",
                    this);

                enabled = false;
            }
        }

        private void Update()
        {
            RefreshContext();
        }

        private void OnDisable()
        {
            if (Instance != this)
                return;

            contextKnown = false;
            engine = null;
            session = default;

            CloseAllWidgets();
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            widgetHost = null;
        }

        private void RefreshContext()
        {
            if (!isActiveAndEnabled || library == null)
                return;

            GameSaveData currentSave = SaveManager.Instance != null
                ? SaveManager.Instance.CurrentSave
                : null;

            string currentUserId =
                AuthenticationManager.Instance != null
                    ? AuthenticationManager.Instance.CurrentUser?.UserId
                    : null;

            bool saveChanged =
                !ReferenceEquals(currentSave, boundSave);

            if (contextKnown &&
                !saveChanged &&
                string.Equals(
                    currentUserId,
                    boundUserId,
                    StringComparison.Ordinal))
            {
                return;
            }

            contextKnown = true;
            boundSave = currentSave;
            boundUserId = currentUserId;
            generation++;

            engine = null;
            session = default;

            if (saveChanged)
                transientData = new StickyNotesSaveData();

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                StickyNotesSaveData store;

                if (boundSave != null)
                {
                    boundSave.stickyNotes ??= new StickyNotesSaveData();
                    store = boundSave.stickyNotes;
                }
                else
                {
                    store = transientData;
                }

                store.users ??= new List<StickyNotesUserData>();

                StickyNotesUserData userData = null;

                foreach (StickyNotesUserData candidate in store.users)
                {
                    if (candidate != null &&
                        candidate.userId == currentUserId)
                    {
                        userData = candidate;
                        break;
                    }
                }

                if (userData == null)
                {
                    userData = new StickyNotesUserData
                    {
                        userId = currentUserId
                    };

                    store.users.Add(userData);
                }

                engine = new StickyNotesEngine(userData, library);

                session = new StickyNotesSession(
                    currentUserId,
                    generation);
            }

            // Context is already replaced before closing old widgets.
            CloseAllWidgets();
            Changed?.Invoke();
        }

        public bool IsCurrentSession(StickyNotesSession expected)
        {
            RefreshContext();

            return isActiveAndEnabled &&
                engine != null &&
                expected.IsValid &&
                expected.Equals(session);
        }

        private bool CheckSession(
            StickyNotesSession expected,
            out string error)
        {
            if (IsCurrentSession(expected))
            {
                error = null;
                return true;
            }

            error = "The user or save has changed. Reopen the note.";
            return false;
        }

        public List<StickyNoteData> GetNotes(
            StickyNotesSession expected,
            string query = null)
        {
            return IsCurrentSession(expected)
                ? engine.GetNotes(query)
                : new List<StickyNoteData>();
        }

        public bool TryGetNote(
            StickyNotesSession expected,
            string noteId,
            out StickyNoteData note)
        {
            note = null;

            return IsCurrentSession(expected) &&
                engine.TryGetNote(noteId, out note);
        }

        public bool TryCreateNote(
            StickyNotesSession expected,
            out StickyNoteData note,
            out string error)
        {
            note = null;

            if (!CheckSession(expected, out error))
                return false;

            note = engine.CreateNote();
            Changed?.Invoke();

            return true;
        }

        public bool TryDeleteNote(
            StickyNotesSession expected,
            string noteId,
            out string error)
        {
            if (!CheckSession(expected, out error))
                return false;

            if (!engine.DeleteNote(noteId))
            {
                error = "This note no longer exists.";
                return false;
            }

            try
            {
                if (HasWidgetHost)
                    widgetHost.CloseNote(expected, noteId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            Changed?.Invoke();
            return true;
        }

        public bool TrySetColor(
            StickyNotesSession expected,
            string noteId,
            StickyNoteColor color,
            out string error)
        {
            if (!CheckSession(expected, out error))
                return false;

            if (!engine.SetColor(noteId, color))
            {
                error = "The note or requested color is unavailable.";
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryUpdateDocument(
            StickyNotesSession expected,
            string noteId,
            string text,
            IReadOnlyList<StickyNoteSpan> spans,
            out string error,
            string stamp = null)
        {
            if (!CheckSession(expected, out error))
                return false;

            if (!engine.UpdateDocument(noteId, text, spans, stamp))
            {
                error = "This note no longer exists.";
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        public bool TryOpenNote(
            StickyNotesSession expected,
            string noteId,
            out string error)
        {
            if (!CheckSession(expected, out error))
                return false;

            if (!engine.TryGetNote(noteId, out _))
            {
                error = "This note no longer exists.";
                return false;
            }

            if (!HasWidgetHost)
            {
                error =
                    "The note is available in the list. " +
                    "The desktop note widget is not connected yet.";

                return false;
            }

            try
            {
                return widgetHost.TryOpenNote(
                    expected,
                    noteId,
                    out error);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                error = "The desktop note could not be opened.";
                return false;
            }
        }

        public void RegisterWidgetHost(IStickyNotesWidgetHost host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            RefreshContext();

            if (HasWidgetHost && !ReferenceEquals(widgetHost, host))
            {
                throw new InvalidOperationException(
                    "A Sticky Notes widget host is already registered.");
            }

            widgetHost = host;
        }

        public void UnregisterWidgetHost(IStickyNotesWidgetHost host)
        {
            if (ReferenceEquals(widgetHost, host))
                widgetHost = null;
        }

        private void CloseAllWidgets()
        {
            if (!HasWidgetHost)
                return;

            try
            {
                widgetHost.CloseAllNotes();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}