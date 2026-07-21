using UnityEngine;

namespace Simulation
{
    public abstract class Actor : MonoBehaviour
    {
        [SerializeField] private int ticksPerAction = 1;
        private int _counter;

        protected WorldContext World { get; private set; }

        // Where the actor logically stands, whatever the transform is drawing part-way through a step.
        // One notion of an actor's cell, so anything asking what is on a cell gets the same answer.
        public abstract Vector2Int Cell { get; }

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
