using UnityEngine;

namespace Simulation
{
    // Drives the scheduler at a fixed logical tick rate, independent of frame rate.
    [RequireComponent(typeof(Scheduler))]
    public class SimulationClock : MonoBehaviour
    {
        [SerializeField, Range(1, 60)] private int ticksPerSecond = 10;

        // After a frame hitch, catch up at most this many ticks — beyond it the sim slows
        // rather than bursting a pile of ticks (and leaps) into one frame.
        [SerializeField, Min(1)] private int maxCatchUpTicks = 3;
        
        private Scheduler _scheduler;
        private float _accumulator;

        // How far this frame sits between the last tick and the next, in [0, 1).
        // Actors interpolate their render by it, so motion is smooth yet paced by the even tick.
        public float Alpha => _accumulator * ticksPerSecond;

        private void Awake() => _scheduler = GetComponent<Scheduler>();

        private void Update()
        {
            // The world stands still while anything holds the lock.
            if (GameLock.Locked) return;

            var step = 1f / ticksPerSecond;
            _accumulator += Mathf.Min(Time.deltaTime, step * maxCatchUpTicks); // cap catch-up so a hitch can't leap
            while (_accumulator >= step)
            {
                _accumulator -= step;
                _scheduler.Tick();
            }
        }
    }
}