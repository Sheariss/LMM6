using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.Splash
{
    public sealed class SplashBinder
    {
        public VisualElement UniversitySplash { get; }
        public VisualElement TeamSplash { get; }
        public VisualElement CapstoneSplash { get; }
        public VisualElement AccessibilitySplash { get; }

        public SplashBinder(VisualElement root)
        {
            UniversitySplash = root.Q<VisualElement>("Splash-university");
            TeamSplash = root.Q<VisualElement>("Splash-team");
            CapstoneSplash = root.Q<VisualElement>("Splash-capstone");
            AccessibilitySplash = root.Q<VisualElement>("Splash-accessibility");
        }
    }
}