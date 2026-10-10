using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.AuthoredData.Users
{
    [CreateAssetMenu(
        fileName = "UserLibrary",
        menuName = "ATLAS/SOS/User Library")]
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
            [SerializeField] private Sprite blurWallpaperHD;
            [SerializeField] private Sprite blurWallpaperBig;

            [Header("Availability")]
            [SerializeField] private bool initiallyHidden;

            [Header("Authentication")]
            [SerializeField] private string password;

            [Header("App Access")]
            [SerializeField] private List<string> availableAppIDs = new();

            [Header("Desktop Placement")]
            [SerializeField] private List<string> desktopShortcutAppIDs = new();
            [SerializeField] private List<string> pinnedAppIDs = new();

            public string UserId => userId;
            public string DisplayName => displayName;
            public UserLoginMethod LoginMethod => loginMethod;

            public Sprite ProfileImage => profileImage;
            public Sprite Wallpaper => wallpaper;
            public Sprite BlurWallpaperHD => blurWallpaperHD;
            public Sprite BlurWallpaperBig => blurWallpaperBig;

            public bool InitiallyHidden => initiallyHidden;

            public string Password => password;

            public IReadOnlyList<string> AvailableAppIDs => availableAppIDs;
            public IReadOnlyList<string> DesktopShortcutAppIDs =>
                desktopShortcutAppIDs;
            public IReadOnlyList<string> PinnedAppIDs => pinnedAppIDs;

            public bool CanAccessApp(string appID)
            {
                return !string.IsNullOrWhiteSpace(appID) &&
                       availableAppIDs.Contains(appID);
            }
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