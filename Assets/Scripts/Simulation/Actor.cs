using UnityEngine;

namespace Simulation
{
    // A participant in the simulation: it takes a turn on the shared tick, every N ticks.
    public abstract class Actor : MonoBehaviour
    {
        [SerializeField] private int ticksPerAction = 1;

        private int _counter;

        public void Tick()
        {
            if (++_counter < ticksPerAction) return;
            _counter = 0;
            Act();
        }

        protected abstract void Act();
    }
}