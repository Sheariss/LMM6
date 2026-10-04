using System;
using Atlas.AuthoredData.Users;
using UnityEngine;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.SOS.Authentication
{
    public enum AuthenticationResult
    {
        Success,
        AccountUnavailable,
        PasswordRequired,
        IncorrectPassword,
        AccountNotConfigured
    }

    public sealed class AuthenticationManager : MonoBehaviour
    {
        public static AuthenticationManager Instance { get; private set; }

        [SerializeField] private UserLibrarySO userLibrary;
        [SerializeField] private string initialUserId = "admin";

        public UserLibrarySO UserLibrary => userLibrary;
        public UserInfo CurrentUser { get; private set; }
        public string LastActiveUserId { get; private set; }
        public bool IsSignedIn => CurrentUser != null;

        public event Action<UserInfo> SignedIn;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LastActiveUserId = initialUserId;
            DontDestroyOnLoad(gameObject);
        }

        public AuthenticationResult TrySignIn(string userId, string password)
        {
            if (userLibrary == null)
                return AuthenticationResult.AccountNotConfigured;

            UserInfo user = userLibrary.GetUser(userId);

            if (user == null || user.InitiallyHidden)
                return AuthenticationResult.AccountUnavailable;

            switch (user.LoginMethod)
            {
                case UserLoginMethod.None:
                    break;

                case UserLoginMethod.Password:
                    if (string.IsNullOrEmpty(user.Password))
                        return AuthenticationResult.AccountNotConfigured;

                    if (string.IsNullOrEmpty(password))
                        return AuthenticationResult.PasswordRequired;

                    if (!string.Equals(
                            password,
                            user.Password,
                            StringComparison.Ordinal))
                    {
                        return AuthenticationResult.IncorrectPassword;
                    }
                    break;

                default:
                    return AuthenticationResult.AccountNotConfigured;
            }

            CurrentUser = user;
            LastActiveUserId = user.UserId;
            SignedIn?.Invoke(user);

            return AuthenticationResult.Success;
        }

        public void SignOut()
        {
            CurrentUser = null;
            // Keep LastActiveUserId for the next login presentation.
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}