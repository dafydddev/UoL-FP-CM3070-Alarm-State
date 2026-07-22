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

        // Currency earned this run but not yet banked.
        // It rides along with the run across levels and is lost with it, unless the run is completed.
        public int PendingCurrency { get; private set; }

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

        // Adds to the pending total for a completed objective or a cleared level.
        public void Award(int amount) => PendingCurrency += amount;
    }
}