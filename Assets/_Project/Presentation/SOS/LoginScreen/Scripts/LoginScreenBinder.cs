using System;
using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public class LoginScreenBinder : UIBinder
    {
        // -------------------- UI ELEMENTS --------------------
        public VisualElement Root { get; }

        public Image SelectedUserWallpaper { get; }


        public GroupBox UserListGroup { get; }

        public Button SelectedUserButton {  get; }
        public Image SelectedProfilePic { get; }
        public Label SelectedUserName { get; }

        public Button User1Button { get; }
        public Image ProfilePic1 { get; }
        public Label UserName1 { get; }

        public Button User2Button { get; }
        public Image ProfilePic2 { get; }
        public Label UserName2 { get; }


        public Image LoginUserImage { get; }
        public Label LoginUserName { get; }

        public GroupBox LoginMethod {  get; }
        public GroupBox PasswordGroup { get; }
        public TextField Password { get; }
        public Button ForgotPasswordButton { get; }


        public Button SignInButton { get; }

        public Button AccessibilityButton { get; }
        public Button PowerButton { get; }



        // -------------------- CONSTRUCTOR --------------------
        public LoginScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[BootScreenBinder] Root VisualElement is null.");
                return;
            }

            SelectedUserWallpaper = Bind<Image>(root, "UserWallpaper");

            Root = Bind<VisualElement>(root, "LoginScreenRoot");

            UserListGroup = Bind<GroupBox>(Root, "UserListGroup");

            SelectedUserButton = Bind<Button>(UserListGroup, "ULB-SelectedUser");
            SelectedProfilePic = Bind<Image>(SelectedUserButton, "ProfilePic");
            SelectedUserName = Bind<Label>(SelectedUserButton, "Username");

            User1Button = Bind<Button>(UserListGroup, "ULB-User1");
            ProfilePic1 = Bind<Image>(User1Button, "ProfilePic");
            UserName1 = Bind<Label>(User1Button, "Username");

            User2Button = Bind<Button>(UserListGroup, "ULB-User2");
            ProfilePic2 = Bind<Image>(User2Button, "ProfilePic");
            UserName2 = Bind<Label>(User2Button, "Username");

            LoginUserImage = Bind<Image>(Root, "LoginUserImage");
            LoginUserName = Bind<Label>(Root, " LoginUserName");

            LoginMethod = Bind<GroupBox>(Root, "LoginMethod");

            PasswordGroup = Bind<GroupBox>(Root, " PasswordGroup");
            Password = Bind<TextField>(Root, "PasswordInputField");
            ForgotPasswordButton = Bind<Button>(Root, "ForgotPasswordButton");

            SignInButton = Bind<Button>(Root, "SignInButton");

            AccessibilityButton = Bind<Button>(Root, "AccessibilityButton");
            PowerButton = Bind<Button>(Root, "PowerButton");
        }

        public void BindActions(
            Action onUser1Pressed,
            Action onUser2Pressed,
            Action onSignInPressed,
            Action onForgotPasswordPressed,
            Action onAccessibilityPressed,
            Action onPowerPressed)
        {
            User1Button.clicked += onUser1Pressed;
            User2Button.clicked += onUser2Pressed;

            SignInButton.clicked += onSignInPressed;
            ForgotPasswordButton.clicked += onForgotPasswordPressed;

            AccessibilityButton.clicked += onAccessibilityPressed;
            PowerButton.clicked += onPowerPressed;
        }
    }
}