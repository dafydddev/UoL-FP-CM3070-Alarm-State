using UnityEngine;

namespace Simulation
{
    // Freezes the game while anything holds a lock (level transition, pause, death...).
    // Counted so overlapping holders can't unfreeze each other: Locked stays true
    // until every hold has been released. The clock and any input handled outside
    // the tick loop check Locked themselves.
    public static class GameLock
    {
        private static int _holds;

        public static bool Locked => _holds > 0;

        public static void Acquire() => _holds++;

        public static void Release() => _holds = Mathf.Max(0, _holds - 1);

        // Drops all holds; called on gameplay-scene entry so a hold leaked by a
        // previous visit (e.g. quitting to the menu while paused) can't freeze a new run.
        public static void Clear() => _holds = 0;
    }
}