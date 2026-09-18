namespace Atlas.Development
{
    public static class DevelopmentContext
    {
        private static DevelopmentSettings settings;

        public static void Initialize(
            DevelopmentSettings developmentSettings)
        {
            settings = developmentSettings;
        }

        public static bool IsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return settings != null &&
                       settings.Enabled;
#else
                return false;
#endif
            }
        }

        public static DevelopmentStartPoint StartPoint =>
            IsEnabled
                ? settings.StartPoint
                : DevelopmentStartPoint.Normal;

        public static float GetSplashDuration(
            float normalDuration)
        {
            if (!IsEnabled)
            {
                return normalDuration;
            }

            return normalDuration *
                   settings.SplashDurationMultiplier;
        }
    }
}