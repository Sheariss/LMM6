using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notepad
{
    public sealed class NotepadView
    {
        private readonly NotepadBinder binder;
        private readonly VisualElement documentMenu;
        private readonly ScrollView documentList;

        private VisualElement currentMenu;

        public bool MenuOpen => currentMenu != null;

        public NotepadView(NotepadBinder binder)
        {
            this.binder = binder;

            foreach (Button button in binder.Buttons)
            {
                if (!button.name.StartsWith("Window"))
                {
                    button.SetEnabled(false);
                    button.tooltip =
                        "Unavailable in the plain-text vertical slice.";
                }
            }

            Hide("FormattingToolbar");
            Hide("HorizontalRuler");
            Hide("VerticalRuler");
            Hide("PageHeader");
            Hide("PageFooter");

            VisualElement horizontalRuler =
                binder.Required<VisualElement>("HorizontalRuler");

            if (horizontalRuler.parent != null)
                horizontalRuler.parent.style.display = DisplayStyle.None;

            VisualElement pages =
                binder.Required<VisualElement>("PagesContainer");

            pages.style.width = Length.Percent(100);
            pages.style.flexGrow = 1;

            VisualElement page =
                binder.Required<VisualElement>("PageOne");

            page.style.width = Length.Percent(100);
            page.style.height = StyleKeyword.Auto;
            page.style.minHeight = 400;
            page.style.paddingLeft = 24;
            page.style.paddingRight = 24;
            page.style.paddingTop = 24;
            page.style.paddingBottom = 24;

            binder.Editor.style.flexShrink = 0;
            binder.Editor.style.minHeight = 350;
            binder.Editor.maxLength = NotepadEngine.MaximumTextLength;
            binder.FileName.maxLength = 100;

            binder.PageCount.text = "Plain text";
            binder.AutoSave.tooltip =
                "Save changes in memory for this game session.";

            binder.Button("Save").text = "Save";
            binder.Button("Save").style.width = 48;
            binder.Button("Undo").text = "↶";
            binder.Button("Redo").text = "↷";
            binder.Button("ReadMode").text = "Read";
            binder.Button("ReadMode").style.width = 48;

            documentMenu = new VisualElement
            {
                name = "NotepadDocumentsMenu"
            };

            documentMenu.AddToClassList("notes-popup-menu");
            documentMenu.style.width = 380;

            Label heading = new("Session documents and drafts");
            heading.AddToClassList("notes-status-label");
            documentMenu.Add(heading);

            documentList = new ScrollView();
            documentList.style.maxHeight = 260;
            documentMenu.Add(documentList);

            binder.MenuLayer.Add(documentMenu);
        }

        public VisualElement DocumentMenu => documentMenu;

        public void Render(NotepadViewState state)
        {
            // Do not reassign unchanged text: preserve caret and selection.
            if (binder.Editor.value != state.Text)
                binder.Editor.SetValueWithoutNotify(state.Text);

            if (binder.FileName.value != state.Name)
                binder.FileName.SetValueWithoutNotify(state.Name);

            binder.Title.text =
                $"{state.Name}{(state.IsDirty ? " *" : string.Empty)} — Notepad";

            binder.Editor.isReadOnly = !state.CanEdit;
            binder.FileName.isReadOnly = !state.CanEdit;

            binder.SaveState.text = state.SaveState;
            binder.AutoSaveState.text = state.AutoSave ? "On" : "Off";

            binder.AutoSave.SetValueWithoutNotify(state.AutoSave);
            binder.AutoSave.SetEnabled(state.CanSave);

            binder.SavedBadge.EnableInClassList(
                "notes-hidden", state.IsDirty);

            binder.UnsavedBadge.EnableInClassList(
                "notes-hidden", !state.IsDirty);

            binder.Button("WordCount").text = state.Statistics;

            SetEnabled(state.CanUndo, "Undo", "ToolbarUndo", "MenuUndo");
            SetEnabled(state.CanRedo, "Redo", "ToolbarRedo", "MenuRedo");
            SetEnabled(state.CanSave, "Save", "MenuSave");
            SetEnabled(state.CanEdit, "Cut", "Paste", "DeleteSelection", "Clear");

            binder.Button("ReadMode").EnableInClassList(
                "is-active", state.ReadMode);

            binder.ZoomSlider.SetValueWithoutNotify(state.ZoomPercent);
            binder.ZoomPercentage.SetValueWithoutNotify(
                $"{state.ZoomPercent}%");

            binder.Editor.style.fontSize = 16f * state.ZoomPercent / 100f;

            binder.Status.text = state.Message;
            binder.Status.EnableInClassList(
                "is-visible", !string.IsNullOrEmpty(state.Message));
        }

        public void ShowMenu(string menuName, Button anchor)
        {
            VisualElement menu =
                binder.Required<VisualElement>(menuName);

            if (currentMenu == menu)
            {
                CloseMenu();
                return;
            }

            Open(menu, anchor);
        }

        public void ShowDocuments(
            IEnumerable<Atlas.SOS.Notepad.NotepadDocument> documents)
        {
            documentList.Clear();
            bool any = false;

            foreach (var document in documents)
            {
                any = true;

                Button button = new()
                {
                    text = document.Name +
                        (document.IsDirty ? " · draft" : string.Empty) +
                        (document.IsReadOnly ? " · read only" : string.Empty),

                    userData = document.Id,
                    tooltip = document.SourcePath ?? "Session document"
                };

                button.AddToClassList("notes-menu-item");
                button.AddToClassList("notes-session-document");
                documentList.Add(button);
            }

            if (!any)
                documentList.Add(new Label("No session documents."));

            Open(documentMenu, binder.Button("FileMenuButton"));
        }

        public bool ContainsPopup(VisualElement target)
        {
            return currentMenu != null &&
                   target != null &&
                   (target == currentMenu || currentMenu.Contains(target));
        }

        public void CloseMenu()
        {
            currentMenu?.RemoveFromClassList("is-open");
            currentMenu = null;

            binder.MenuLayer.RemoveFromClassList("is-open");
        }

        public void ShowPrompt(bool visible)
        {
            binder.Prompt.style.display =
                visible ? DisplayStyle.Flex : DisplayStyle.None;

            binder.PromptText.text = "Clear the document text?";
        }

        public void Dispose()
        {
            CloseMenu();
            documentMenu.RemoveFromHierarchy();
        }

        private void Open(VisualElement menu, Button anchor)
        {
            CloseMenu();

            currentMenu = menu;
            binder.MenuLayer.AddToClassList("is-open");
            menu.AddToClassList("is-open");

            menu.BringToFront();
            binder.MenuLayer.BringToFront();

            Vector2 position = binder.MenuLayer.WorldToLocal(
                new Vector2(
                    anchor.worldBound.xMin,
                    anchor.worldBound.yMax));

            float width = binder.MenuLayer.resolvedStyle.width;
            float left = float.IsFinite(width)
                ? Mathf.Clamp(position.x, 0, Mathf.Max(0, width - 380))
                : position.x;

            menu.style.left = left;
            menu.style.top = Mathf.Max(0, position.y);
        }

        private void SetEnabled(bool enabled, params string[] names)
        {
            foreach (string name in names)
                binder.Button(name).SetEnabled(enabled);
        }

        private void Hide(string name)
        {
            binder.Required<VisualElement>(name).style.display =
                DisplayStyle.None;
        }
    }
}