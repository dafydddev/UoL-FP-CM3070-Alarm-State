using UnityEngine;

namespace Simulation
{
    public abstract class Actor : MonoBehaviour
    {
        [SerializeField] private int ticksPerAction = 1;
        private int _counter;

        protected WorldContext World { get; private set; }

        // Called by the spawner after Instantiate. OnEnable has already run by then
        // (with no context), so registration happens here for the first activation.
        public virtual void Init(WorldContext world)
        {
            World = world;
            if (isActiveAndEnabled) world.Scheduler.Register(this);
        }

        protected virtual void OnEnable()
        {
            World?.Scheduler.Register(this);
        }

        protected virtual void OnDisable()
        {
            World?.Scheduler.Unregister(this);
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
