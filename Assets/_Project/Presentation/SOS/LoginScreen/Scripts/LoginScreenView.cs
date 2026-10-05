using UnityEngine.UIElements;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenView
    {
        private readonly LoginScreenBinder binder;
        private int focusVersion;

        public LoginScreenView(LoginScreenBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- RENDER --------------------
        public void Apply(ViewState state)
        {
            ApplySelectedUser(state.SelectedUser);

            ApplyUserSlot(binder.User1Button, binder.ProfilePic1, binder.UserName1, state.User1);
            ApplyUserSlot(binder.User2Button, binder.ProfilePic2, binder.UserName2, state.User2);

            SetDisplay(binder.CredentialsSection, state.Section == LoginScreenSection.Credentials);
            SetDisplay(binder.LoginMessageSection, state.Section == LoginScreenSection.Message);

            SetDisplay(binder.PasswordGroup, state.ShowPassword);
            SetDisplay(binder.SignInButton, state.ShowSignIn);

            // These require additional authentication support.
            SetDisplay(binder.AttemptsSection, false);
            SetDisplay(binder.RecoverySection, false);
            SetDisplay(binder.ResetPasswordSection, false);

            binder.PasswordSubmitButton.SetEnabled(state.CanSubmit);
            binder.SignInButton.SetEnabled(state.CanSubmit);

            binder.LoginMessageLabel.text = state.Message;
            binder.LoginErrorLabel.text = state.InlineError;

            SetDisplay(binder.LoginErrorLabel, !string.IsNullOrEmpty(state.InlineError));
        }

        private void ApplySelectedUser(UserInfo user)
        {
            bool hasUser = user != null;

            SetDisplay(binder.SelectedUserButton, hasUser);
            SetDisplay(binder.LoginUserImage, hasUser);

            binder.LoginUserName.text = user?.DisplayName ?? "No available accounts";
            binder.SelectedUserName.text = user?.DisplayName ?? string.Empty;

            binder.LoginUserImage.sprite = user?.ProfileImage;
            binder.SelectedProfilePic.sprite = user?.ProfileImage;
            binder.SelectedUserWallpaper.sprite = user?.Wallpaper;
        }

        private static void ApplyUserSlot(Button button, Image image, Label label, UserInfo user)
        {
            SetDisplay(button, user != null);

            if (user == null)
                return;

            image.sprite = user.ProfileImage;
            label.text = user.DisplayName;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // -------------------- INPUT --------------------
        public void ClearInputs()
        {
            binder.Password.SetValueWithoutNotify(string.Empty);
            binder.ChallengeInputField.SetValueWithoutNotify(string.Empty);

            binder.RecoveryAnswer1Field.SetValueWithoutNotify(string.Empty);
            binder.RecoveryAnswer2Field.SetValueWithoutNotify(string.Empty);
            binder.RecoveryAnswer3Field.SetValueWithoutNotify(string.Empty);

            binder.NewPasswordInputField.SetValueWithoutNotify(string.Empty);
            binder.ConfirmPasswordInputField.SetValueWithoutNotify(string.Empty);
        }

        public void FocusPrimary(ViewState state)
        {
            int version = ++focusVersion;
            VisualElement target = null;

            if (state.Section == LoginScreenSection.Message)
                target = binder.LoginMessageOkButton;
            else if (state.ShowPassword)
                target = binder.Password;
            else if (state.ShowSignIn)
                target = binder.SignInButton;

            if (target == null)
                return;

            // Wait for section visibility changes to reach layout.
            binder.Root.schedule.Execute(() =>
            {
                if (version != focusVersion || binder.Root.panel == null)
                    return;

                if (binder.Root.resolvedStyle.display == DisplayStyle.None)
                    return;

                if (target.enabledInHierarchy)
                    target.Focus();
            });
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
                focusVersion++;

            SetDisplay(binder.Root, visible);
        }

        // -------------------- VIEW STATE --------------------
        public sealed class ViewState
        {
            public UserInfo SelectedUser { get; set; }
            public UserInfo User1 { get; set; }
            public UserInfo User2 { get; set; }

            public LoginScreenSection Section { get; set; }

            public bool ShowPassword { get; set; }
            public bool ShowSignIn { get; set; }
            public bool CanSubmit { get; set; }

            public string Message { get; set; }
            public string InlineError { get; set; }
        }
    }
}