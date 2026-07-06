using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    // Holds the actors and advances them one logical tick at a time.
    public class Scheduler : MonoBehaviour
    {
        private readonly List<Actor> _actors = new();

        public void Register(Actor actor) => _actors.Add(actor);
        public void Unregister(Actor actor) => _actors.Remove(actor);

        public void Tick()
        {
            for (var i = _actors.Count - 1; i >= 0; i--) _actors[i].Tick();
        }
    }
}