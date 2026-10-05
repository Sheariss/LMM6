using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenBinder : UIBinder
    {
        // -------------------- ROOT --------------------

        public VisualElement Root { get; }
        public VisualElement Content { get; }
        public GroupBox LoginArea { get; }
        public Image SelectedUserWallpaper { get; }

        // -------------------- ACCOUNT LIST --------------------

        public GroupBox UserListGroup { get; }
        public Button SelectedUserButton { get; }
        public Image SelectedProfilePic { get; }
        public Label SelectedUserName { get; }
        public Button User1Button { get; }
        public Image ProfilePic1 { get; }
        public Label UserName1 { get; }
        public Button User2Button { get; }
        public Image ProfilePic2 { get; }
        public Label UserName2 { get; }

        // -------------------- ACCOUNT IDENTITY --------------------

        public Image LoginUserImage { get; }
        public Label LoginUserName { get; }

        // -------------------- CREDENTIALS --------------------

        public GroupBox LoginMethod { get; }
        public GroupBox CredentialsSection { get; }
        public GroupBox PasswordGroup { get; }
        public TextField Password { get; }
        public Button PasswordSubmitButton { get; }
        public Button ForgotPasswordButton { get; }
        public Label LoginErrorLabel { get; }
        public Button SignInButton { get; }

        // -------------------- LOGIN MESSAGE --------------------

        public GroupBox LoginMessageSection { get; }
        public Label LoginMessageLabel { get; }
        public Button LoginMessageOkButton { get; }

        // -------------------- ATTEMPT CHALLENGE --------------------

        public GroupBox AttemptsSection { get; }
        public Label AttemptsMessageLabel { get; }
        public Label ChallengePromptLabel { get; }
        public TextField ChallengeInputField { get; }
        public Button ChallengeSubmitButton { get; }
        public Label ChallengeErrorLabel { get; }

        // -------------------- RECOVERY --------------------

        public GroupBox RecoverySection { get; }
        public VisualElement RecoveryQuestionsContainer { get; }
        public Label RecoveryQuestion1Label { get; }
        public Label RecoveryQuestion2Label { get; }
        public Label RecoveryQuestion3Label { get; }
        public TextField RecoveryAnswer1Field { get; }
        public TextField RecoveryAnswer2Field { get; }
        public TextField RecoveryAnswer3Field { get; }
        public Button RecoverySubmitButton { get; }
        public Label RecoveryErrorLabel { get; }
        public Button RecoveryCancelButton { get; }

        // -------------------- PASSWORD RESET --------------------

        public GroupBox ResetPasswordSection { get; }
        public TextField NewPasswordInputField { get; }
        public TextField ConfirmPasswordInputField { get; }
        public Label ResetPasswordErrorLabel { get; }
        public Button ResetPasswordSubmitButton { get; }
        public Button ResetPasswordCancelButton { get; }

        // -------------------- SYSTEM CONTROLS --------------------

        public GroupBox IconListGroup { get; }
        public Button AccessibilityButton { get; }
        public Button PowerButton { get; }

        // -------------------- CONSTRUCTOR --------------------

        public LoginScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                IsValid = false;
                Debug.LogError("[LoginScreenBinder] Root VisualElement is null.");
                return;
            }

            // Root.
            Root = Bind<VisualElement>(root, "LoginScreenRoot");
            Content = Bind<VisualElement>(root, "LoginScreenContent");
            LoginArea = Bind<GroupBox>(root, "LoginArea");
            SelectedUserWallpaper = Bind<Image>(root, "UserWallpaper");

            // Account list.
            UserListGroup = Bind<GroupBox>(root, "UserListGroup");

            // These names repeat, so queries must use their account button.
            SelectedUserButton = Bind<Button>(root, "ULB-SelectedUser");
            SelectedProfilePic = Bind<Image>(SelectedUserButton, "ProfilePic");
            SelectedUserName = Bind<Label>(SelectedUserButton, "Username");

            User1Button = Bind<Button>(root, "ULB-User1");
            ProfilePic1 = Bind<Image>(User1Button, "ProfilePic");
            UserName1 = Bind<Label>(User1Button, "Username");

            User2Button = Bind<Button>(root, "ULB-User2");
            ProfilePic2 = Bind<Image>(User2Button, "ProfilePic");
            UserName2 = Bind<Label>(User2Button, "Username");

            // Account identity.
            LoginUserImage = Bind<Image>(root, "LoginUserImage");
            LoginUserName = Bind<Label>(root, "LoginUserName");

            // Credentials.
            LoginMethod = Bind<GroupBox>(root, "LoginMethod");
            CredentialsSection = Bind<GroupBox>(root, "CredentialsSection");
            PasswordGroup = Bind<GroupBox>(root, "PasswordGroup");
            Password = Bind<TextField>(root, "PasswordInputField");
            PasswordSubmitButton = Bind<Button>(root, "PasswordSubmitButton");
            ForgotPasswordButton = Bind<Button>(root, "ForgotPasswordButton");
            LoginErrorLabel = Bind<Label>(root, "LoginErrorLabel");
            SignInButton = Bind<Button>(root, "SignInButton");

            // Login message.
            LoginMessageSection = Bind<GroupBox>(root, "LoginMessageSection");
            LoginMessageLabel = Bind<Label>(root, "LoginMessageLabel");
            LoginMessageOkButton = Bind<Button>(root, "LoginMessageOkButton");

            // Attempt challenge.
            AttemptsSection = Bind<GroupBox>(root, "AttemptsSection");
            AttemptsMessageLabel = Bind<Label>(root, "AttemptsMessageLabel");
            ChallengePromptLabel = Bind<Label>(root, "ChallengePromptLabel");
            ChallengeInputField = Bind<TextField>(root, "ChallengeInputField");
            ChallengeSubmitButton = Bind<Button>(root, "ChallengeSubmitButton");
            ChallengeErrorLabel = Bind<Label>(root, "ChallengeErrorLabel");

            // Recovery.
            RecoverySection = Bind<GroupBox>(root, "RecoverySection");
            RecoveryQuestionsContainer = Bind<VisualElement>(root, "RecoveryQuestionsContainer");
            RecoveryQuestion1Label = Bind<Label>(root, "RecoveryQuestion1Label");
            RecoveryQuestion2Label = Bind<Label>(root, "RecoveryQuestion2Label");
            RecoveryQuestion3Label = Bind<Label>(root, "RecoveryQuestion3Label");
            RecoveryAnswer1Field = Bind<TextField>(root, "RecoveryAnswer1Field");
            RecoveryAnswer2Field = Bind<TextField>(root, "RecoveryAnswer2Field");
            RecoveryAnswer3Field = Bind<TextField>(root, "RecoveryAnswer3Field");
            RecoverySubmitButton = Bind<Button>(root, "RecoverySubmitButton");
            RecoveryErrorLabel = Bind<Label>(root, "RecoveryErrorLabel");
            RecoveryCancelButton = Bind<Button>(root, "RecoveryCancelButton");

            // Password reset.
            ResetPasswordSection = Bind<GroupBox>(root, "ResetPasswordSection");
            NewPasswordInputField = Bind<TextField>(root, "NewPasswordInputField");
            ConfirmPasswordInputField = Bind<TextField>(root, "ConfirmPasswordInputField");
            ResetPasswordErrorLabel = Bind<Label>(root, "ResetPasswordErrorLabel");
            ResetPasswordSubmitButton = Bind<Button>(root, "ResetPasswordSubmitButton");
            ResetPasswordCancelButton = Bind<Button>(root, "ResetPasswordCancelButton");

            // System controls.
            IconListGroup = Bind<GroupBox>(root, "IconListGroup");
            AccessibilityButton = Bind<Button>(root, "AccessibilityButton");
            PowerButton = Bind<Button>(root, "PowerButton");
        }
    }
}