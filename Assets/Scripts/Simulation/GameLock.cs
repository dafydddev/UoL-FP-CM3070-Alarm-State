using UnityEngine;

namespace Simulation
{
    // Freezes the game while anything holds a lock (level transition, pause, death...).
    // Counted so overlapping holders can't unfreeze each other: Locked stays true until every hold has been released.
    // The clock and any input handled outside the tick loop check Locked themselves.
    public static class GameLock
    {
        private static int _holds;
        private static int _releasedFrame = -1;

        // Stays true for the rest of the frame the last hold was dropped on,
        // so the key that dismissed one holder cannot be read by the next thing to open.
        public static bool Locked => _holds > 0 || _releasedFrame == Time.frameCount;

        public static void Acquire() => _holds++;

        public static void Release()
        {
            _holds = Mathf.Max(0, _holds - 1);
            if (_holds == 0) _releasedFrame = Time.frameCount;
        }

        // Drops all holds; called on gameplay-scene entry, so a hold leaked by a previous visit can't freeze a new run.
        // A fresh run starts running, so this skips the grace frame a release grants.
        public static void Clear()
        {
            _holds = 0;
            _releasedFrame = -1;
        }
    }
}