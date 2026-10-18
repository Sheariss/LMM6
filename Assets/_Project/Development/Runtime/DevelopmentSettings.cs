using UnityEngine;

namespace Atlas.Development
{
    public enum DevelopmentStartPoint
    {
        Normal = 0,
        MainMenu = 1,
        NewGame = 2,
        SOS = 3,
        Credits = 4,
        GuestDesktop = 5
    }

    [CreateAssetMenu(
        fileName = "DevelopmentSettings",
        menuName = "ATLAS/Development/Development Settings"
    )]
    public sealed class DevelopmentSettings : ScriptableObject
    {
        [Header("Development")]
        [SerializeField]
        private bool enabled = true;

        [Header("Startup")]
        [SerializeField]
        private DevelopmentStartPoint startPoint =
            DevelopmentStartPoint.GuestDesktop;

        [Header("Splash")]
        [SerializeField]
        [Range(0f, 2f)]
        private float splashDurationMultiplier = 1f;

        public bool Enabled => enabled;

        public DevelopmentStartPoint StartPoint => startPoint;

        public float SplashDurationMultiplier =>
            splashDurationMultiplier;
    }
}