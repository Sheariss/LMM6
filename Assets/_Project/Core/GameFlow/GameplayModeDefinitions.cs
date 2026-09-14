namespace Atlas.Core.GameState
{
    /// <summary>
    /// Represents the active interaction context while gameplay is occurring.
    /// </summary>
    public enum GameplayMode
    {
        None,
        Tutorial,
        Investigating,
        Dialogue,
        Cutscene
    }
}