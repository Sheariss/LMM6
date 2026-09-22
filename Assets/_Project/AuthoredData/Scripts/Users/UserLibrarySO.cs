using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.AuthoredData.Users
{
    [CreateAssetMenu(
        fileName = "UserLibrary",
        menuName = "SOS/User Library")]
    public class UserLibrarySO : ScriptableObject
    {
        public enum UserLoginMethod
        {
            None,
            Password
        }

        [Serializable]
        public class UserInfo
        {
            [Header("Identity")]
            [SerializeField] private string userId;
            [SerializeField] private string displayName;
            [SerializeField] private UserLoginMethod loginMethod;

            [Header("Appearance")]
            [SerializeField] private Sprite profileImage;
            [SerializeField] private Sprite wallpaper;

            [Header("Availability")]
            [SerializeField] private bool initiallyHidden;

            public string UserId => userId;
            public string DisplayName => displayName;
            public UserLoginMethod LoginMethod => loginMethod;

            public Sprite ProfileImage => profileImage;
            public Sprite Wallpaper => wallpaper;

            public bool InitiallyHidden => initiallyHidden;
        }

        [SerializeField]
        private List<UserInfo> users = new();

        public IReadOnlyList<UserInfo> Users => users;

        public UserInfo GetUser(string userId)
        {
            foreach (UserInfo user in users)
            {
                if (user.UserId == userId)
                    return user;
            }

            Debug.LogWarning($"[UserLibrarySO] User '{userId}' was not found.");

            return null;
        }
    }
}