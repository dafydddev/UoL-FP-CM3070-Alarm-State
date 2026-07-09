namespace Run
{
    public sealed class RunContext
    {
        public int CurrentLevel { get; private set; }
        public int TotalLevels { get; }

        private bool IsComplete => CurrentLevel >= TotalLevels;

        public RunContext(int startLevel, int totalLevels)
        {
            CurrentLevel = startLevel;
            TotalLevels = totalLevels;
        }

        public bool Advance()
        {
            if (IsComplete) return false;
            CurrentLevel++; return true;
        }
    }
}