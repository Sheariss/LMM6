using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.Splash
{
    public sealed class SplashBinder
    {
        public VisualElement UniversitySplash { get; }
        public VisualElement TeamSplash { get; }
        public VisualElement CapstoneSplash { get; }
        public VisualElement AccessibilitySplash { get; }

        // -------------------- STATE --------------------
        public bool IsValid { get; }

        // -------------------- CONSTRUCTOR --------------------
        public SplashBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[SplashBinder] Root VisualElement is null.");

                IsValid = false;
                return;
            }

            UniversitySplash = root.Q<VisualElement>("Splash-university");
            TeamSplash = root.Q<VisualElement>("Splash-team");
            CapstoneSplash = root.Q<VisualElement>("Splash-capstone");
            AccessibilitySplash = root.Q<VisualElement>("Splash-accessibility");

            IsValid = VerifyBindings();
        }

        // -------------------- VERIFICATION --------------------
        private bool VerifyBindings()
        {
            bool isValid = true;

            if (UniversitySplash == null)
            {
                Debug.LogError("[SplashBinder] Missing required element: " + "'Splash-university'.");
                isValid = false;
            }

            if (TeamSplash == null)
            {
                Debug.LogError("[SplashBinder] Missing required element: " + "'Splash-team'.");
                isValid = false;
            }

            if (CapstoneSplash == null)
            {
                Debug.LogError("[SplashBinder] Missing required element: " + "'Splash-capstone'.");
                isValid = false;
            }

            if (AccessibilitySplash == null)
            {
                Debug.LogError("[SplashBinder] Missing required element: " + "'Splash-accessibility'.");
                isValid = false;
            }

            if (isValid)
            {
                Debug.Log( "[SplashBinder] All required UI bindings verified.");
            }

            return isValid;
        }
    }
}