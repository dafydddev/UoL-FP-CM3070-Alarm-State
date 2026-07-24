using Generation.Tiles;

namespace Run
{
    public sealed class RunContext
    {
        // Set by the menu before loading the gameplay scene.
        // Null when the scene was entered directly, LevelOrchestrator builds a default from its inspector values.
        public static RunContext Pending;

        public RunDifficulty Profile { get; }
        public TileLayoutStyle LayoutStyle { get; }
        public int CurrentLevel { get; private set; }
        public int TotalLevels { get; }

        // What each source has paid the run so far, kept apart so the results screen can break the takings down.
        public int LevelClearedEarnings { get; private set; }
        public int PrimaryObjectiveEarnings { get; private set; }
        public int SecondaryObjectiveEarnings { get; private set; }

        // The whole pending purse: it rides along across levels and is lost with the run, unless the run is completed.
        public int PendingCurrency => LevelClearedEarnings + PrimaryObjectiveEarnings + SecondaryObjectiveEarnings;

        private bool IsComplete => CurrentLevel >= TotalLevels;

        public RunContext(RunDifficulty profile, int startLevel, int totalLevels,
            TileLayoutStyle layoutStyle = TileLayoutStyle.Spine)
        {
            Profile = profile;
            CurrentLevel = startLevel;
            TotalLevels = totalLevels;
            LayoutStyle = layoutStyle;
        }

        public bool Advance()
        {
            if (IsComplete) return false;
            CurrentLevel++;
            return true;
        }

        public void AwardLevelCleared(int amount) => LevelClearedEarnings += amount;

        public void AwardPrimaryObjective(int amount) => PrimaryObjectiveEarnings += amount;

        public void AwardSecondaryObjective(int amount) => SecondaryObjectiveEarnings += amount;
    }
}
