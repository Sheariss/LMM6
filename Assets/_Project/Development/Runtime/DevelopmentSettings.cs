using UnityEngine;

namespace Atlas.Development
{
    public enum DevelopmentStartPoint
    {
        Normal,
        MainMenu,
        NewGame,
        SOS
    }

    [CreateAssetMenu(
        fileName = "DevelopmentSettings",
        menuName = "ATLAS/Development/Development Settings"
    )]
    public sealed class DevelopmentSettings : ScriptableObject
    {
        // -------------------- GENERAL --------------------

        [Header("Development")]
        [SerializeField]
        private bool enabled = true;

        // -------------------- STARTUP --------------------

        [Header("Startup")]
        [SerializeField]
        private DevelopmentStartPoint startPoint = DevelopmentStartPoint.Normal;

        // -------------------- SPLASH --------------------

        [Header("Splash")]
        [SerializeField]
        [Range(0f, 2f)]
        private float splashDurationMultiplier = 1f;

        // -------------------- PROPERTIES --------------------

        public bool Enabled =>
            enabled;

        public DevelopmentStartPoint StartPoint =>
            startPoint;

        public float SplashDurationMultiplier =>
            splashDurationMultiplier;
    }
}