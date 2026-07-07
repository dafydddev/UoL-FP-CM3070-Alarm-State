using UnityEngine;

namespace Simulation
{
    public abstract class Actor : MonoBehaviour
    {
        [SerializeField] private int ticksPerAction = 1;
        private int _counter;

        protected virtual void OnEnable()
        {
            if (World.Instance) World.Instance.Scheduler.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (World.Instance) World.Instance.Scheduler.Unregister(this);
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