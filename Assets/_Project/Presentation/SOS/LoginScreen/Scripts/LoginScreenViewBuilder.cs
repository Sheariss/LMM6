using Atlas.SOS.Authentication;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenViewBuilder
    {
        public LoginScreenView.ViewState Build(LoginScreenEngine engine)
        {
            UserInfo selectedUser = engine.SelectedUser;

            bool showWelcome = engine.Section == LoginScreenSection.Welcome;
            bool showCredentials = engine.Section == LoginScreenSection.Credentials;
            bool showMessage = engine.Section == LoginScreenSection.Message;
            bool canInteract = !showWelcome;

            bool requiresPassword =
                selectedUser != null &&
                selectedUser.LoginMethod == UserLoginMethod.Password;

            bool passwordFree =
                selectedUser != null &&
                selectedUser.LoginMethod == UserLoginMethod.None;

            var state = new LoginScreenView.ViewState
            {
                SelectedUser = selectedUser,
                Section = engine.Section,

                ShowCredentials = showCredentials,
                ShowMessage = showMessage,
                ShowWelcome = showWelcome,

                ShowPassword = showCredentials && requiresPassword,
                ShowSignIn = showCredentials && passwordFree,

                CanInteract = canInteract,
                CanSubmit =
                    canInteract &&
                    showCredentials &&
                    (requiresPassword || passwordFree),

                Message = showMessage ? GetMessage(engine) : string.Empty,
                InlineError = showCredentials
                    ? GetInlineError(engine)
                    : string.Empty
            };

            // The selected account already occupies its own row.
            foreach (UserInfo user in engine.AvailableUsers)
            {
                if (user == null)
                    continue;

                if (selectedUser != null && user.UserId == selectedUser.UserId)
                    continue;

                if (state.User1 == null)
                {
                    state.User1 = user;
                    continue;
                }

                state.User2 = user;
                break;
            }

            return state;
        }

        private static string GetMessage(LoginScreenEngine engine)
        {
            if (engine.RecoveryUnavailable)
                return "Password recovery is not available for this account.";

            switch (engine.LastResult)
            {
                case AuthenticationResult.IncorrectPassword:
                    return "The password is incorrect. Try again.";

                case AuthenticationResult.AccountUnavailable:
                    return "This account is unavailable.";

                case AuthenticationResult.AccountNotConfigured:
                    return "This account is not configured for sign-in.";

                default:
                    return string.Empty;
            }
        }

        private static string GetInlineError(LoginScreenEngine engine)
        {
            if (engine.SelectedUser == null)
                return "No accounts are available.";

            if (engine.SelectedUser.LoginMethod != UserLoginMethod.Password &&
                engine.SelectedUser.LoginMethod != UserLoginMethod.None)
            {
                return "This sign-in method is not supported.";
            }

            return engine.LastResult == AuthenticationResult.PasswordRequired
                ? "Enter your password."
                : string.Empty;
        }
    }
}