using Atlas.AuthoredData.Users;
using UnityEngine;
using UnityEngine.UIElements;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenView
    {
        // -------------------- BINDER --------------------
        private readonly LoginScreenBinder binder;


        // -------------------- CONSTRUCTOR --------------------

        public LoginScreenView(LoginScreenBinder binder)
        {
            this.binder = binder;
        }


        // -------------------- VIEW --------------------
        public void Apply(ViewState state)
        {
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
        }


        // -------------------- SELECTED USER --------------------
        private void ApplySelectedUser(UserInfo user)
        {
            if (user == null)
            {
                return;
            }

            binder.SelectedUserName.text = user.DisplayName;

            binder.SelectedProfilePic.sprite = user.ProfileImage;

            binder.SelectedUserWallpaper.sprite = user.Wallpaper;

            binder.LoginUserName.text = user.DisplayName;

            binder.LoginUserImage.sprite = user.ProfileImage;

            ApplyLoginMethod(user);
        }


        // -------------------- USER SLOTS --------------------
        private static void ApplyUserSlot(Button button, Image profileImage, Label userName, UserInfo user)
        {
            bool visible = user != null;

            button.style.display = visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (!visible)
            {
                return;
            }

            profileImage.sprite = user.ProfileImage;

            userName.text = user.DisplayName;
        }


        // -------------------- LOGIN METHOD --------------------
        private void ApplyLoginMethod(UserInfo user)
        {
            bool requiresPassword = user.LoginMethod == UserLoginMethod.Password;

            binder.LoginMethod.style.display = DisplayStyle.Flex;

            binder.PasswordGroup.style.display = requiresPassword
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            binder.Password.value = string.Empty;
        }

        // -------------------- VIEW STATE --------------------
        public sealed class ViewState
        {
            public UserInfo SelectedUser { get; set; }
            public UserInfo User1 { get; set; }
            public UserInfo User2 { get; set; }
        }
    }
}