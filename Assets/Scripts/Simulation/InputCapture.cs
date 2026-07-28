using UnityEngine;

namespace Simulation
{
    // Holds the player's keys for a screen that reads them itself, without freezing the world.
    public static class InputCapture
    {
        private static int _holds;
        private static int _releasedFrame = -1;

        // Stays true for the rest of the frame the last hold was dropped on.
        public static bool Captured => _holds > 0 || _releasedFrame == Time.frameCount;

        public static void Acquire() => _holds++;

        public static void Release()
        {
            _holds = Mathf.Max(0, _holds - 1);
            if (_holds == 0) _releasedFrame = Time.frameCount;
        }

        // Drops all holds; called on gameplay-scene entry.
        public static void Clear()
        {
            _holds = 0;
            _releasedFrame = -1;
        }
    }
}
