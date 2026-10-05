using System.Collections.Generic;
using Atlas.SOS.Authentication;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public enum LoginScreenSection
    {
        Credentials,
        Message
    }

    public sealed class LoginScreenEngine
    {
        private readonly AuthenticationManager authentication;
        private readonly List<UserInfo> availableUsers = new List<UserInfo>();

        public IReadOnlyList<UserInfo> AvailableUsers => availableUsers;
        public UserInfo SelectedUser { get; private set; }
        public LoginScreenSection Section { get; private set; }
        public AuthenticationResult? LastResult { get; private set; }
        public bool RecoveryUnavailable { get; private set; }

        public LoginScreenEngine(AuthenticationManager authentication)
        {
            this.authentication = authentication;
        }

        // -------------------- INITIALIZATION --------------------

        public void Initialize()
        {
            RefreshAvailableUsers();

            SelectedUser = FindAvailableUser(authentication.LastActiveUserId);

            if (SelectedUser == null && availableUsers.Count > 0)
                SelectedUser = availableUsers[0];

            ResetPresentation();
        }

        private void RefreshAvailableUsers()
        {
            availableUsers.Clear();

            foreach (UserInfo user in authentication.UserLibrary.Users)
            {
                if (user != null && !user.InitiallyHidden && !string.IsNullOrWhiteSpace(user.UserId))
                    availableUsers.Add(user);
            }
        }

        private UserInfo FindAvailableUser(string userId)
        {
            foreach (UserInfo user in availableUsers)
            {
                if (user.UserId == userId)
                    return user;
            }

            return null;
        }

        // -------------------- ACCOUNT SELECTION --------------------

        public bool SelectUser(string userId)
        {
            RefreshAvailableUsers();

            UserInfo user = FindAvailableUser(userId);

            if (user == null || user == SelectedUser)
                return false;

            SelectedUser = user;
            ResetPresentation();

            return true;
        }

        // -------------------- AUTHENTICATION --------------------

        public bool TrySignIn(string password)
        {
            if (Section != LoginScreenSection.Credentials)
                return false;

            RecoveryUnavailable = false;

            if (SelectedUser == null)
            {
                LastResult = AuthenticationResult.AccountUnavailable;
                Section = LoginScreenSection.Message;
                return false;
            }

            string submittedPassword = SelectedUser.LoginMethod == UserLoginMethod.Password
                ? password
                : string.Empty;

            AuthenticationResult result = authentication.TrySignIn(SelectedUser.UserId, submittedPassword);
            LastResult = result;

            switch (result)
            {
                case AuthenticationResult.Success:
                    return true;

                case AuthenticationResult.PasswordRequired:
                    Section = LoginScreenSection.Credentials;
                    return false;

                default:
                    Section = LoginScreenSection.Message;
                    return false;
            }
        }

        public void RequestRecovery()
        {
            if (SelectedUser == null || SelectedUser.LoginMethod != UserLoginMethod.Password)
                return;

            LastResult = null;
            RecoveryUnavailable = true;
            Section = LoginScreenSection.Message;
        }

        public void DismissMessage()
        {
            RefreshAvailableUsers();

            SelectedUser = FindAvailableUser(SelectedUser?.UserId);

            if (SelectedUser == null && availableUsers.Count > 0)
                SelectedUser = availableUsers[0];

            ResetPresentation();
        }

        private void ResetPresentation()
        {
            Section = LoginScreenSection.Credentials;
            LastResult = null;
            RecoveryUnavailable = false;
        }
    }
}