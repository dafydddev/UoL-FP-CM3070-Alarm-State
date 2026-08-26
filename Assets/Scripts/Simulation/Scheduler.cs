using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    // Holds the actors and advances them one logical tick at a time.
    public class Scheduler : MonoBehaviour
    {
        private readonly List<Actor> _actors = new();

        // The live actors, so cell rules can see who is standing where without a second register to keep in step.
        public IReadOnlyList<Actor> Actors => _actors;

        public void Register(Actor actor) => _actors.Add(actor);
        public void Unregister(Actor actor) => _actors.Remove(actor);

        // Backwards, so an actor unregistering itself mid-tick does not shift one out of the pass.
        public void Tick()
        {
            for (var i = _actors.Count - 1; i >= 0; i--) _actors[i].Tick();
        }
    }
}