using UnityEngine;

namespace Simulation
{
    // Drives the scheduler at a fixed logical tick rate, independent of frame rate.
    [RequireComponent(typeof(Scheduler))]
    public class SimulationClock : MonoBehaviour
    {
        [SerializeField, Range(1, 60)] private int ticksPerSecond = 10;

        private Scheduler _scheduler;
        private float _accumulator;

        private void Awake() => _scheduler = GetComponent<Scheduler>();

        private void Update()
        {
            var step = 1f / ticksPerSecond;
            _accumulator += Time.deltaTime;
            while (_accumulator >= step)
            {
                _accumulator -= step;
                _scheduler.Tick();
            }
        }
    }
    
}