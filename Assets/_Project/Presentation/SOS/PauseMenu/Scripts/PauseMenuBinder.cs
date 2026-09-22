using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.GameUI.PauseMenu
{
    public class PauseMenuBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Button ResumeButton { get; }
        public Button SaveButton { get; }
        public Button LoadButton { get; }
        public Button SettingsButton { get; }
        public Button AccessibilityButton { get; }
        public Button HelpButton { get; }
        public Button MainMenuButton { get; }
        public Button ExitGameButton { get; }

        public PauseMenuBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[PauseBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "PauseMenuRoot");

            ResumeButton = Bind<Button>(root, "PM-ResumeGameButton");
            SaveButton = Bind<Button>(root, "PM-SaveGameButton");
            LoadButton = Bind<Button>(root, "PM-LoadGameButton");
            SettingsButton = Bind<Button>(root, "PM-SettingsButton");
            AccessibilityButton = Bind<Button>(root, "PM-AccessibilityButton");
            HelpButton = Bind<Button>(root, "PM-HelpButton");
            MainMenuButton = Bind<Button>(root, "PM-MainMenuButton");
            ExitGameButton = Bind<Button>(root, "PM-ExitGameButton");
        }
    }
}