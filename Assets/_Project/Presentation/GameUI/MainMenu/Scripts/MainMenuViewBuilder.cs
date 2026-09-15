namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuViewBuilder
    {
        private readonly SaveManager saveManager;
        private readonly ProgressionManager progressionManager;

        public MainMenuViewBuilder(SaveManager saveManager, ProgressionManager progressionManager)
        {
            this.saveManager = saveManager;
            this.progressionManager = progressionManager;
        }

        public MainMenuView.ViewState Build()
        {
            bool hasSaveData = saveManager.GetSaveCount() > 0;

            bool gameCompleted = progressionManager.IsCheckpointCompleted(
                    // TODO: Replace with actual final checkpoint identifier
                );

            return new MainMenuView.ViewState
            {
                ContinueVisible = hasSaveData,
                CreditsVisible = gameCompleted
            };
        }
    }
}