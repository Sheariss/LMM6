namespace Atlas.Core.GameState
{
    /// <summary>
    /// Represents the current high-level operational state of the IIS.
    /// </summary>
    public enum GameFlowState
    {
        Booting,
        Splash,
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        Credits
    }
}