using Generation.Tiles;

namespace Run
{
    public sealed class RunContext
    {
        // Set by the menu before loading the gameplay scene.
        // Null when the scene was entered directly, LevelOrchestrator builds a default from its inspector values.
        public static RunContext Pending;

        public RunDifficulty DifficultyProfile { get; }
        public TileLayoutStyle LayoutStyle { get; }
        public int CurrentLevel { get; private set; }
        public int TotalLevels { get; }

        // What each source has paid the run so far, kept apart so that the result screen can break the takings down.
        public int PrimaryObjectiveEarnings { get; private set; }
        public int SecondaryObjectiveEarnings { get; private set; }
        public int RunCompletedEarnings { get; private set; }

        // The whole pending purse: it rides along across levels and is lost with the run, unless the run is completed.
        public int PendingCurrency => PrimaryObjectiveEarnings + SecondaryObjectiveEarnings + RunCompletedEarnings;

        // What this run's difficulty pays, at the length this run was started at.
        public int PrimaryObjectiveReward => DifficultyProfile.primaryObjectiveReward;
        public int SecondaryObjectiveReward => DifficultyProfile.secondaryObjectiveReward;
        public int RunCompletedReward => DifficultyProfile.RunCompletedReward(TotalLevels);

        private bool IsComplete => CurrentLevel >= TotalLevels;

        public RunContext(RunDifficulty difficultyProfile, int startLevel, int totalLevels,
            TileLayoutStyle layoutStyle = TileLayoutStyle.Spine)
        {
            DifficultyProfile = difficultyProfile;
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

        public void AwardPrimaryObjective(int amount) => PrimaryObjectiveEarnings += amount;

        public void AwardSecondaryObjective(int amount) => SecondaryObjectiveEarnings += amount;

        public void AwardRunCompleted(int amount) => RunCompletedEarnings += amount;
    }
}
