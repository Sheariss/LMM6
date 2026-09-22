using Atlas.AuthoredData.Users;
using System.Collections.Generic;
using UnityEngine;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public class LoginScreenViewBuilder : MonoBehaviour
    {
        // -------------------- AUTHORED DATA --------------------
        private readonly UserLibrarySO userLibrary;

        // -------------------- CONSTRUCTOR --------------------
        public LoginScreenViewBuilder(UserLibrarySO userLibrary)
        {
            this.userLibrary = userLibrary;
        }

        // -------------------- BUILD --------------------
        public LoginScreenView.ViewState Build(UserInfo selectedUser)
        {
            List<UserInfo> visibleUsers = GetVisibleUsers();

            return new LoginScreenView.ViewState
            {
                SelectedUser = selectedUser,
                User1 = GetUserAtIndex(visibleUsers, 0),
                User2 = GetUserAtIndex(visibleUsers, 1)
            };
        }

        // -------------------- USER VISIBILITY --------------------
        public bool IsUserVisible(UserInfo user)
        {
            if (user == null)
            {
                return false;
            }
            return !user.InitiallyHidden;
            // Later: return !user.InitiallyHidden || progressionManager.IsUserUnlocked(user.UserId);
        }

        private List<UserInfo> GetVisibleUsers()
        {
            List<UserInfo> visibleUsers = new List<UserInfo>();

            foreach (UserInfo user in userLibrary.Users)
            {
                if (!IsUserVisible(user))
                {
                    continue;
                }
                visibleUsers.Add(user);
            }
            return visibleUsers;
        }

        // -------------------- USER LOOKUP --------------------
        public UserInfo GetVisibleUser(int index)
        {
            List<UserInfo> visibleUsers = GetVisibleUsers();

            return GetUserAtIndex(visibleUsers, index);
        }

        public UserInfo GetFirstVisibleUser()
        {
            return GetVisibleUser(0);
        }

        private static UserInfo GetUserAtIndex(List<UserInfo> users, int index)
        {
            if (index < 0 || index >= users.Count)
            {
                return null;
            }
            return users[index];
        }
    }
}