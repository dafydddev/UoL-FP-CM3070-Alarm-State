using UnityEngine;

namespace Simulation
{
    // A participant in the simulation: it takes a turn on the shared tick, every N ticks.
    public abstract class Actor : MonoBehaviour
    {
        [SerializeField] private int ticksPerAction = 1;

        private int _counter;
        private Scheduler _scheduler;

        // Joins the simulation so the scheduler drives this actor's turns.
        public virtual void Bind(Scheduler scheduler)
        {
            _scheduler = scheduler;
            scheduler.Register(this);
        }

        protected virtual void OnDestroy()
        {
            if (_scheduler) _scheduler.Unregister(this);
        }

        public void Tick()
        {
            if (++_counter < ticksPerAction) return;
            _counter = 0;
            Act();
        }

        protected abstract void Act();
    }
}