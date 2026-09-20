using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.GameUI.SecurityScreen
{
    public class SecurityScreenBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Button LockButton { get; }
        public Button SwitchUserButton { get; }
        public Button SignOutButton { get; }
        public Button ChangePasswordButton { get; }
        public Button TaskManagerButton { get; }
        public Button CancelButton { get; }

        public SecurityScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[PauseBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "SecurityScreenRoot");

            LockButton = Bind<Button>(root, "WS-LockButton");
            SwitchUserButton = Bind<Button>(root, "WS-SwitchUserButton");
            SignOutButton = Bind<Button>(root, "WS-SignOutButton");
            ChangePasswordButton = Bind<Button>(root, "WS-ChangePasswordButton");
            TaskManagerButton = Bind<Button>(root, "WS-TaskManagerButton");
            CancelButton = Bind<Button>(root, "WS-CancelButton");
        }
    }
}

