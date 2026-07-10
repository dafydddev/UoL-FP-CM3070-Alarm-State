using Generation;

namespace Run
{
    public sealed class RunContext
    {
        // Set by the menu before loading the gameplay scene;
        // null when the scene was entered directly, LevelOrchestrator builds a default from its inspector values.
        // Cleared by the consumer after reading.
        public static RunContext Pending;

        public RunDifficultyProfile Profile { get; }
        public int CurrentLevel { get; private set; }
        public int TotalLevels { get; }

        private bool IsComplete => CurrentLevel >= TotalLevels;

        public RunContext(RunDifficultyProfile profile, int startLevel, int totalLevels)
        {
            Profile = profile;
            CurrentLevel = startLevel;
            TotalLevels = totalLevels;
        }

        public bool Advance()
        {
            if (IsComplete) return false;
            CurrentLevel++;
            return true;
        }
    }
}