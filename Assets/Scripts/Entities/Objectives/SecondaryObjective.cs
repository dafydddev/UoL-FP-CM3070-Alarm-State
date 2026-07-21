using UnityEngine;

namespace Entities.Objectives
{
    // An optional side objective.
    // It has no bearing on the mission, so hacking it pays the player in an item instead: see ObjectiveDrop.
    [RequireComponent(typeof(ObjectiveDrop))]
    public class SecondaryObjective : Objective
    {
        // Seed for the reward roll, stamped by the spawner alongside the hacking seed.
        public int dropSeed;

        private ObjectiveDrop _drop;

        private void Awake() => _drop = GetComponent<ObjectiveDrop>();

        protected override void OnHacked() => _drop.Roll(World, dropSeed);
    }
}
