using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.Splash
{
    public enum SplashType
    {
        University,
        Team,
        Capstone,
        Accessibility
    }
    public sealed class SplashView
    {
        private readonly SplashBinder binder;

        public SplashView(SplashBinder binder)
        {
            this.binder = binder;
        }

        public void Show(SplashType splash)
        {
            HideAll();

            switch (splash)
            {
                case SplashType.University:
                    binder.UniversitySplash.style.display =
                        DisplayStyle.Flex;
                    break;

                case SplashType.Team:
                    binder.TeamSplash.style.display =
                        DisplayStyle.Flex;
                    break;

                case SplashType.Capstone:
                    binder.CapstoneSplash.style.display =
                        DisplayStyle.Flex;
                    break;

                case SplashType.Accessibility:
                    binder.AccessibilitySplash.style.display =
                        DisplayStyle.Flex;
                    break;
            }
        }

        public void HideAll()
        {
            binder.UniversitySplash.style.display = DisplayStyle.None;
            binder.TeamSplash.style.display = DisplayStyle.None;
            binder.CapstoneSplash.style.display = DisplayStyle.None;
            binder.AccessibilitySplash.style.display = DisplayStyle.None;
        }
    }
}
