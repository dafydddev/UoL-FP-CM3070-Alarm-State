using System;
using UnityEngine;

namespace Entities.Objectives
{
    // An optional side objective.
    // It has no bearing on the mission, so completing it pays the player in an item instead: see ObjectiveDrop.
    [RequireComponent(typeof(ObjectiveDrop))]
    public class SecondaryObjective : Objective
    {
        // Raised when a secondary objective is completed. Static so the run orchestrator can subscribe once.
        public static event Action Completed;

        // Seed for the reward roll, stamped by the spawner alongside the minigame seed.
        public int dropSeed;

        public override MiniGameType Game => MiniGameType.Sequence;

        private ObjectiveDrop _drop;

        private void Awake() => _drop = GetComponent<ObjectiveDrop>();

        protected override void OnWon()
        {
            _drop.Roll(World, dropSeed);
            Completed?.Invoke();
        }
    }
}