using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.Authentication;
using Atlas.SOS.FileSystem;
using Atlas.SOS.Notepad;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notepad
{
    public sealed class NotepadController :
        AppContentController,
        IFileContentReceiver
    {
        [Header("Standalone fallback")]
        [SerializeField] private string fallbackUserId = "guest";

        private NotepadManager manager;
        private NotepadBinder binder;
        private NotepadEngine engine;
        private NotepadViewBuilder builder;
        private NotepadView view;

        private string userId;
        private int sessionEpoch;
        private bool autoSave;
        private bool clearPrompt;
        private string message;

        private readonly List<Button> menuAnchors = new();

        protected override void OnInitialize()
        {
            manager = NotepadManager.Instance;

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Add NotepadManager to the desktop scene.");
            }

            userId = ResolveUserId();

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "Notepad requires a user ID.");
            }

            sessionEpoch = manager.Epoch;

            binder = new NotepadBinder(InstanceRoot);
            engine = new NotepadEngine();
            builder = new NotepadViewBuilder();
            view = new NotepadView(binder);

            if (manager.TryGetLast(userId, out var document))
                engine.Load(document);
            else
                engine.NewDocument();

            RegisterCallbacks();

            message =
                "Documents and drafts are retained for this game session.";

            Stage();
        }

        protected override void OnShutdown()
        {
            // Edits have already been staged as they occur.
            view?.Dispose();
            menuAnchors.Clear();

            manager = null;
            binder = null;
            engine = null;
            builder = null;
            view = null;
        }

        public bool TryOpenFile(
            FileOpenRequest request,
            out string error)
        {
            error = null;

            if (!IsInitialized || !CanUse())
            {
                error = "Notepad is unavailable for the current session.";
                return false;
            }

            if (request?.File == null ||
                request.File.FileType != FileSystemFileType.Text ||
                request.File.Hidden ||
                string.IsNullOrWhiteSpace(request.File.Id) ||
                string.IsNullOrWhiteSpace(request.FilePath))
            {
                error = "Notepad requires a visible, valid text file.";
                return false;
            }

            if (!string.Equals(
                request.UserId,
                userId,
                StringComparison.Ordinal))
            {
                error = "This file belongs to a different user session.";
                return false;
            }

            string text = NotepadEngine.Normalize(
                request.File.TextContent);

            if (text.Length > NotepadEngine.MaximumTextLength)
            {
                error =
                    $"This text exceeds the {NotepadEngine.MaximumTextLength}" +
                    "-character vertical-slice limit.";

                return false;
            }

            string name = string.IsNullOrWhiteSpace(request.File.Name)
                ? "Text document"
                : request.File.Name;

            engine.Load(new NotepadDocument
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Text = text,
                SourcePath = request.FilePath,
                IsReadOnly = request.File.ReadOnly,

                HasBaseline = true,
                SavedName = name,
                SavedText = text
            });

            CancelPrompt();
            view.CloseMenu();

            message = request.File.ReadOnly
                ? "Read-only authored file. Use Save as to create an editable copy."
                : "Saving retains a session copy; the authored file is unchanged.";

            Stage();
            return true;
        }

        private void RegisterCallbacks()
        {
            foreach (string menu in new[]
            {
                "File", "Edit", "View", "Tools", "Help"
            })
            {
                string panelName = menu + "Menu";
                Button anchor = binder.Button(menu + "MenuButton");

                menuAnchors.Add(anchor);

                Wire(anchor.name, () =>
                {
                    view.ShowMenu(panelName, anchor);
                });
            }

            Wire("New", NewDocument);
            Wire("Open", ShowDocuments);
            Wire("MakeCopy", () => MakeCopy(false));
            Wire("SaveAs", () => MakeCopy(true));

            Wire("Save", Save);
            Wire("MenuSave", Save);

            Wire("Undo", Undo);
            Wire("ToolbarUndo", Undo);
            Wire("MenuUndo", Undo);

            Wire("Redo", Redo);
            Wire("ToolbarRedo", Redo);
            Wire("MenuRedo", Redo);

            Wire("Cut", () => CopySelection(true));
            Wire("Copy", () => CopySelection(false));
            Wire("Paste", Paste);
            Wire("DeleteSelection", () => ReplaceSelection(string.Empty));
            Wire("SelectAll", () =>
            {
                binder.Editor.Focus();
                binder.Editor.SelectRange(0, engine.Document.Text.Length);
            });

            Wire("Clear", RequestClear);
            Wire("Cancel", CancelPrompt);
            Wire("Confirm", ConfirmClear);

            Wire("Find", FocusSearch);
            Wire("FindNext", () => Find(false));
            Wire("FindPrevious", () => Find(true));

            Wire("ReadMode", ToggleReadMode);
            Wire("MenuReadMode", ToggleReadMode);

            Wire("ZoomIn", () => Zoom(10));
            Wire("MenuZoomIn", () => Zoom(10));
            Wire("ZoomOut", () => Zoom(-10));
            Wire("MenuZoomOut", () => Zoom(-10));

            Wire("ZoomActualSize", () =>
            {
                engine.SetZoom(100);
                Render();
            });

            Wire("WordCount", ShowStatistics);
            Wire("MenuWordCount", ShowStatistics);
            Wire("DocumentDetails", ShowDetails);

            Wire("KeyboardShortcuts", () => Tell(
                "Ctrl+N: New � Ctrl+O: Session documents � Ctrl+S: Save � " +
                "Ctrl+Shift+S: Save a copy � Ctrl+F: Find � " +
                "F3 / Shift+F3: Next / previous match � " +
                "Ctrl+Z / Ctrl+Y: Undo / redo in the editor."));

            Wire("AboutNotepad", () => Tell(
                "Atlas Notepad � Plain-text editor � " +
                "Documents and drafts are held in memory for this session."));

            BindCallback<ChangeEvent<string>>(binder.Editor, evt =>
            {
                if (!CanUse())
                    return;

                if (engine.SetText(evt.newValue))
                    Edited();
                else
                    Render();
            });

            BindCallback<ChangeEvent<string>>(binder.FileName, evt =>
            {
                if (!CanUse())
                    return;

                engine.SetName(evt.newValue);
                Edited();
            });

            BindCallback<ChangeEvent<bool>>(binder.AutoSave, evt =>
            {
                if (!CanUse())
                    return;

                autoSave = evt.newValue &&
                           !engine.Document.IsReadOnly;

                if (autoSave)
                    engine.Save();

                Stage();
            });

            BindCallback<ChangeEvent<float>>(binder.ZoomSlider, evt =>
            {
                if (!CanUse())
                    return;

                engine.SetZoom(Mathf.RoundToInt(evt.newValue));
                Render();
            });

            binder.ZoomPercentage.isDelayed = true;

            BindCallback<ChangeEvent<string>>(
                binder.ZoomPercentage,
                evt =>
                {
                    if (!CanUse())
                        return;

                    if (int.TryParse(
                        evt.newValue.Trim().TrimEnd('%'),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int percentage))
                    {
                        engine.SetZoom(percentage);
                    }

                    Render();
                });

            BindCallback<KeyDownEvent>(
                binder.Root,
                OnKeyDown,
                TrickleDown.TrickleDown);

            BindCallback<PointerDownEvent>(
                binder.Root,
                OnPointerDown,
                TrickleDown.TrickleDown);

            // One delegated callback handles all generated document rows.
            BindCallback<ClickEvent>(view.DocumentMenu, evt =>
            {
                if (!CanUse())
                    return;

                for (VisualElement target = evt.target as VisualElement;
                     target != null && target != view.DocumentMenu;
                     target = target.parent)
                {
                    if (target is Button button &&
                        button.ClassListContains("notes-session-document") &&
                        button.userData is string id)
                    {
                        OpenSessionDocument(id);
                        break;
                    }
                }
            });
        }

        private void Wire(string name, Action action)
        {
            Button button = binder.Button(name);
            button.SetEnabled(true);
            button.tooltip = string.Empty;

            BindClick(button, () =>
            {
                if (!CanUse())
                    return;

                bool isAnchor = menuAnchors.Contains(button);

                if (!isAnchor)
                    view.CloseMenu();

                action();
            });
        }

        private bool CanUse()
        {
            if (manager == null || engine == null || binder == null)
                return false;

            if (manager.Epoch == sessionEpoch &&
                string.Equals(
                    ResolveUserId(),
                    userId,
                    StringComparison.Ordinal))
            {
                return true;
            }

            binder.Editor.isReadOnly = true;
            binder.FileName.isReadOnly = true;

            binder.Status.text =
                "The user or game session changed. Close and reopen Notepad.";

            binder.Status.AddToClassList("is-visible");
            return false;
        }

        private string ResolveUserId()
        {
            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            return authentication != null &&
                   authentication.CurrentUser != null
                ? authentication.CurrentUser.UserId
                : fallbackUserId;
        }

        private void NewDocument()
        {
            engine.NewDocument();
            CancelPrompt();

            message =
                "New document. Previous documents and drafts remain in Open.";

            Stage();
        }

        private void ShowDocuments()
        {
            view.ShowDocuments(manager.GetDocuments(userId));
        }

        private void OpenSessionDocument(string id)
        {
            if (!manager.TryGet(userId, id, out var document))
            {
                Tell("That session document is no longer available.");
                return;
            }

            engine.Load(document);
            CancelPrompt();
            view.CloseMenu();

            message = "Opened a session document.";
            Stage();
        }

        private void MakeCopy(bool saveImmediately)
        {
            engine.MakeCopy();

            if (saveImmediately)
                engine.Save();

            CancelPrompt();

            message = saveImmediately
                ? "Editable copy saved for this session."
                : "Created an editable draft copy.";

            Stage();
        }

        private void Save()
        {
            if (engine.Document.IsReadOnly)
            {
                Tell("Use Save as to create an editable copy.");
                return;
            }

            engine.Save();
            message = "Saved for this session.";
            Stage();
        }

        private void Edited()
        {
            if (autoSave && !engine.Document.IsReadOnly)
            {
                engine.Save();
                message = "Automatically saved for this session.";
            }
            else
            {
                message = "Draft retained. Use Save to mark this version saved.";
            }

            Stage();
        }

        private void Undo()
        {
            if (!engine.CanUndo)
                return;

            engine.Undo();
            Edited();
            FocusEditorAtEnd();
        }

        private void Redo()
        {
            if (!engine.CanRedo)
                return;

            engine.Redo();
            Edited();
            FocusEditorAtEnd();
        }

        private void CopySelection(bool cut)
        {
            if (cut && !engine.CanEdit)
                return;

            GetSelection(out int start, out int end);

            if (start == end)
                return;

            GUIUtility.systemCopyBuffer =
                engine.Document.Text.Substring(start, end - start);

            if (cut)
                ReplaceSelection(string.Empty);
        }

        private void Paste()
        {
            ReplaceSelection(GUIUtility.systemCopyBuffer);
        }

        private void ReplaceSelection(string replacement)
        {
            if (!engine.CanEdit)
                return;

            GetSelection(out int start, out int end);
            replacement = NotepadEngine.Normalize(replacement);

            string text = engine.Document.Text;

            if (text.Length - (end - start) + replacement.Length >
                NotepadEngine.MaximumTextLength)
            {
                Tell("The resulting document would exceed the text limit.");
                return;
            }

            string updated =
                text.Substring(0, start) +
                replacement +
                text.Substring(end);

            if (engine.SetText(updated))
                Edited();

            int caret = start + replacement.Length;

            binder.Editor.Focus();
            binder.Editor.SelectRange(caret, caret);
        }

        private void GetSelection(out int start, out int end)
        {
            int length = engine.Document.Text.Length;

            start = Mathf.Clamp(
                Mathf.Min(
                    binder.Editor.cursorIndex,
                    binder.Editor.selectIndex),
                0,
                length);

            end = Mathf.Clamp(
                Mathf.Max(
                    binder.Editor.cursorIndex,
                    binder.Editor.selectIndex),
                start,
                length);
        }

        private void FocusSearch()
        {
            binder.Search.Focus();
            binder.Search.SelectAll();
        }

        private void Find(bool backwards)
        {
            string query = binder.Search.value ?? string.Empty;

            if (query.Length == 0)
            {
                FocusSearch();
                return;
            }

            GetSelection(out int start, out int end);

            int index = engine.Find(
                query,
                backwards ? start - 1 : end,
                backwards);

            if (index < 0)
            {
                Tell("No matching text was found.");
                return;
            }

            binder.Editor.Focus();
            binder.Editor.SelectRange(index, index + query.Length);

            Tell("Matching text selected.");
        }

        private void RequestClear()
        {
            if (!engine.CanEdit)
                return;

            clearPrompt = true;
            view.ShowPrompt(true);
        }

        private void CancelPrompt()
        {
            clearPrompt = false;
            view.ShowPrompt(false);
        }

        private void ConfirmClear()
        {
            if (!clearPrompt || !engine.CanEdit)
                return;

            CancelPrompt();

            if (engine.SetText(string.Empty))
                Edited();
        }

        private void ToggleReadMode()
        {
            engine.ToggleReadMode();
            Render();
        }

        private void Zoom(int amount)
        {
            engine.SetZoom(engine.ZoomPercent + amount);
            Render();
        }

        private void ShowStatistics()
        {
            NotepadViewState state =
                builder.Build(engine, autoSave, message);

            Tell(state.Statistics);
        }

        private void ShowDetails()
        {
            string source = string.IsNullOrWhiteSpace(
                engine.Document.SourcePath)
                ? "Created in Notepad"
                : engine.Document.SourcePath;

            Tell(
                $"{engine.Document.Name} � Source: {source} � " +
                "Saves and drafts are held in memory for this session.");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!view.MenuOpen)
                return;

            VisualElement target = evt.target as VisualElement;

            if (view.ContainsPopup(target))
                return;

            foreach (Button anchor in menuAnchors)
            {
                if (Within(target, anchor))
                    return;
            }

            view.CloseMenu();

            if (target == binder.MenuLayer)
                evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!CanUse())
                return;

            bool control = evt.ctrlKey || evt.commandKey;
            bool handled = true;

            if (evt.keyCode == KeyCode.Escape)
            {
                view.CloseMenu();
                CancelPrompt();
            }
            else if (evt.keyCode == KeyCode.F3)
            {
                Find(evt.shiftKey);
            }
            else if (evt.keyCode == KeyCode.Return &&
                     Within(evt.target as VisualElement, binder.Search))
            {
                Find(evt.shiftKey);
            }
            else if (control && !evt.altKey)
            {
                bool editorTarget = Within(
                    evt.target as VisualElement,
                    binder.Editor);

                switch (evt.keyCode)
                {
                    case KeyCode.N:
                        NewDocument();
                        break;

                    case KeyCode.O:
                        ShowDocuments();
                        break;

                    case KeyCode.S:
                        if (evt.shiftKey)
                            MakeCopy(true);
                        else
                            Save();
                        break;

                    case KeyCode.F:
                        FocusSearch();
                        break;

                    case KeyCode.Z when editorTarget:
                        if (evt.shiftKey)
                            Redo();
                        else
                            Undo();
                        break;

                    case KeyCode.Y when editorTarget:
                        Redo();
                        break;

                    default:
                        handled = false;
                        break;
                }
            }
            else
            {
                handled = false;
            }

            if (handled)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }

        private void Stage()
        {
            manager.Store(userId, engine.Document);
            Render();
        }

        private void Tell(string text)
        {
            message = text;
            Render();
        }

        private void Render()
        {
            view.Render(builder.Build(engine, autoSave, message));
        }

        private void FocusEditorAtEnd()
        {
            binder.Editor.Focus();

            int end = engine.Document.Text.Length;
            binder.Editor.SelectRange(end, end);
        }

        private static bool Within(
            VisualElement target,
            VisualElement container)
        {
            return target != null &&
                   container != null &&
                   (target == container || container.Contains(target));
        }
    }
}