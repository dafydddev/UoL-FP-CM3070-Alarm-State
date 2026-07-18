using Generation.Tiles;

namespace Run
{
    public sealed class RunContext
    {
        // Set by the menu before loading the gameplay scene;
        // null when the scene was entered directly, LevelOrchestrator builds a default from its inspector values.
        // Cleared by the consumer after reading.
        public static RunContext Pending;

        public RunDifficulty Profile { get; }
        public TileLayoutStyle LayoutStyle { get; }
        public int CurrentLevel { get; private set; }
        public int TotalLevels { get; }

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
    }
}