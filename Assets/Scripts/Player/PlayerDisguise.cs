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

        // Disguised while the clock is still running.
        // Public so guard vision can respect it.
        public bool IsDisguised => _worn > 0f;

        // Puts a disguise on for a stretch of time.
        // Wearing one while another still runs keeps the longer of the two rather than cutting it short.
        public void Wear(float seconds)
        {
            if (seconds <= 0f) return;
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
