using Simulation;
using UnityEngine;

namespace Player
{
    // Tracks the disguise the player is wearing and how much longer it lasts.
    // Separate from PlayerHiding, which counts cover zones: a disguise is worn, not stood in.
    public class PlayerDisguise : MonoBehaviour
    {
        // Seconds of disguise remaining.
        private float _worn;

        // The length the disguise being worn started from, so a full mark is there to measure the rest against.
        private float _span;

        // Disguised while the clock is still running.
        public bool IsDisguised => _worn > 0f;

        // How much of the disguise being worn is left, 0 to 1.
        public float Remaining => _span > 0f ? Mathf.Clamp01(_worn / _span) : 0f;

        // Puts a disguise on for a stretch of time.
        // Wearing one while another still runs keeps the longer of the two rather than cutting it short.
        public void Wear(float seconds)
        {
            if (seconds <= 0f) return;

            // A fresh disguise measures against its own length; one put on over another keeps the longer span.
            _span = IsDisguised ? Mathf.Max(_span, seconds) : seconds;
            _worn = Mathf.Max(_worn, seconds);
        }

        // Runs the disguise down only while the game runs, so a pause doesn't quietly spend it.
        private void Update()
        {
            if (GameLock.Locked) return;
            if (_worn > 0f) _worn -= Time.deltaTime;
        }
    }
}
