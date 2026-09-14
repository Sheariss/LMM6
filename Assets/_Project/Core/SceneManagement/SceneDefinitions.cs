using System;

namespace Atlas.Core.SceneManagement
{
    /// <summary>
    /// Identifies the Unity scenes recognized by the IIS runtime.
    /// </summary>
    public enum GameScene
    {
        Bootstrap,
        Splash,
        MainMenu,
        Loading,
        SOS,
        Credits
    }

    /// <summary>
    /// Provides centralized Unity scene definitions for the IIS runtime.
    /// </summary>
    public static class SceneDefinitions
    {
        public static string GetSceneName(GameScene scene)
        {
            return scene switch
            {
                GameScene.Bootstrap => "Bootstrap",
                GameScene.Splash => "Splash",
                GameScene.MainMenu => "MainMenu",
                GameScene.Loading => "Loading",
                GameScene.SOS => "SOS",
                GameScene.Credits => "Credits",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(scene),
                    scene,
                    "Unknown game scene."
                )
            };
        }
    }
}