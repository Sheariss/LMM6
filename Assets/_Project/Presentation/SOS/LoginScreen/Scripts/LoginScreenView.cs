using UnityEngine;
using UnityEngine.UIElements;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenView
    {
        private const long SpinnerIntervalMilliseconds = 16;
        private const float SpinnerDegreesPerSecond = 270f;

        private readonly LoginScreenBinder binder;

        private IVisualElementScheduledItem spinnerAnimation;
        private double spinnerStartedAt;
        private bool spinnerRunning;
        private bool showWelcome;
        private bool isVisible = true;
        private int focusVersion;

        public LoginScreenView(LoginScreenBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- RENDER --------------------

        public void Apply(ViewState state)
        {
            // Invalidate focus requests from the previous presentation.
            focusVersion++;

            showWelcome = state.ShowWelcome;

            ApplySelectedUser(state.SelectedUser);

            ApplyUserSlot(
                binder.User1Button,
                binder.ProfilePic1,
                binder.UserName1,
                state.User1
            );

            ApplyUserSlot(
                binder.User2Button,
                binder.ProfilePic2,
                binder.UserName2,
                state.User2
            );

            SetDisplay(binder.CredentialsSection, state.ShowCredentials);
            SetDisplay(binder.LoginMessageSection, state.ShowMessage);
            SetDisplay(binder.WelcomeSection, state.ShowWelcome);

            SetDisplay(binder.PasswordGroup, state.ShowPassword);
            SetDisplay(binder.SignInButton, state.ShowSignIn);

            // These require additional authentication support.
            SetDisplay(binder.AttemptsSection, false);
            SetDisplay(binder.RecoverySection, false);
            SetDisplay(binder.ResetPasswordSection, false);

            ApplyInteraction(state);

            binder.WelcomeLabel.text = "Welcome";
            binder.LoginMessageLabel.text = state.Message ?? string.Empty;
            binder.LoginErrorLabel.text = state.InlineError ?? string.Empty;

            SetDisplay(
                binder.LoginErrorLabel,
                state.ShowCredentials &&
                !string.IsNullOrEmpty(state.InlineError)
            );

            UpdateSpinner();
        }

        private void ApplyInteraction(ViewState state)
        {
            // Disable interactive groups without dimming the Welcome section.
            binder.UserListGroup.SetEnabled(state.CanInteract);
            binder.LoginMethod.SetEnabled(state.CanInteract);
            binder.IconListGroup.SetEnabled(state.CanInteract);

            binder.Password.SetEnabled(
                state.CanInteract && state.ShowPassword
            );

            binder.ForgotPasswordButton.SetEnabled(
                state.CanInteract && state.ShowPassword
            );

            binder.PasswordSubmitButton.SetEnabled(
                state.CanInteract && state.CanSubmit && state.ShowPassword
            );

            binder.SignInButton.SetEnabled(
                state.CanInteract && state.CanSubmit && state.ShowSignIn
            );

            binder.LoginMessageOkButton.SetEnabled(
                state.CanInteract && state.ShowMessage
            );
        }

        private void ApplySelectedUser(UserInfo user)
        {
            bool hasUser = user != null;

            SetDisplay(binder.SelectedUserButton, hasUser);
            SetDisplay(binder.LoginUserImage, hasUser);

            binder.LoginUserName.text =
                user?.DisplayName ?? "No available accounts";

            binder.SelectedUserName.text =
                user?.DisplayName ?? string.Empty;

            binder.LoginUserImage.sprite = user?.ProfileImage;
            binder.SelectedProfilePic.sprite = user?.ProfileImage;
            binder.SelectedUserWallpaper.sprite = user?.Wallpaper;
        }

        private static void ApplyUserSlot(
            Button button,
            Image image,
            Label label,
            UserInfo user)
        {
            SetDisplay(button, user != null);

            image.sprite = user?.ProfileImage;
            label.text = user?.DisplayName ?? string.Empty;
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            element.style.display =
                visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // -------------------- SPINNER --------------------

        private void UpdateSpinner()
        {
            if (isVisible && showWelcome)
                StartSpinner();
            else
                StopSpinner();
        }

        private void StartSpinner()
        {
            if (spinnerRunning)
                return;

            spinnerRunning = true;
            spinnerStartedAt = Time.realtimeSinceStartupAsDouble;

            SetSpinnerAngle(0f);

            if (spinnerAnimation == null)
            {
                // UI Toolkit pauses scheduled work while detached from a panel.
                spinnerAnimation = binder.WelcomeSpinner.schedule
                    .Execute(AnimateSpinner)
                    .Every(SpinnerIntervalMilliseconds);
            }
            else
            {
                spinnerAnimation.Resume();
            }
        }

        private void StopSpinner()
        {
            spinnerRunning = false;
            spinnerAnimation?.Pause();
            SetSpinnerAngle(0f);
        }

        private void AnimateSpinner()
        {
            if (!spinnerRunning)
                return;

            double elapsed =
                Time.realtimeSinceStartupAsDouble - spinnerStartedAt;

            float angle =
                (float)((elapsed * SpinnerDegreesPerSecond) % 360.0);

            SetSpinnerAngle(angle);
        }

        private void SetSpinnerAngle(float angle)
        {
            binder.WelcomeSpinner.style.rotate =
                new Rotate(new Angle(angle, AngleUnit.Degree));
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

            if (!isVisible || !state.CanInteract)
                return;

            VisualElement target = null;

            if (state.ShowMessage)
                target = binder.LoginMessageOkButton;
            else if (state.ShowCredentials && state.ShowPassword)
                target = binder.Password;
            else if (state.ShowCredentials && state.ShowSignIn)
                target = binder.SignInButton;

            if (target == null)
                return;

            // Wait for section visibility changes to reach layout.
            binder.Root.schedule.Execute(() =>
            {
                if (version != focusVersion ||
                    !isVisible ||
                    binder.Root.panel == null ||
                    target.panel == null)
                {
                    return;
                }

                if (binder.Root.resolvedStyle.display == DisplayStyle.None)
                    return;

                if (target.enabledInHierarchy)
                    target.Focus();
            });
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;

            if (!visible)
                focusVersion++;

            SetDisplay(binder.Root, visible);
            UpdateSpinner();
        }

        // -------------------- VIEW STATE --------------------

        public sealed class ViewState
        {
            public UserInfo SelectedUser { get; set; }
            public UserInfo User1 { get; set; }
            public UserInfo User2 { get; set; }

            public LoginScreenSection Section { get; set; }

            public bool ShowCredentials { get; set; }
            public bool ShowMessage { get; set; }
            public bool ShowWelcome { get; set; }

            public bool ShowPassword { get; set; }
            public bool ShowSignIn { get; set; }

            public bool CanInteract { get; set; }
            public bool CanSubmit { get; set; }

            public string Message { get; set; }
            public string InlineError { get; set; }
        }
    }
}