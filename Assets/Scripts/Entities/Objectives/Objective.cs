using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities.Objectives
{
    // A mission objective the player completes with the use key while standing on or beside it.
    // Using it opens the pipe hacking minigame; the objective completes once the hack is done.
    // What that completion is worth is left to the two kinds: see PrimaryObjective and SecondaryObjective.
    public abstract class Objective : MonoBehaviour, IUseHandler
    {
        // Fires when the player uses an unhacked objective, so the hacking screen can open for it.
        public static event Action<Objective> HackRequested;

        public string id;

        // Seed for this objective's hacking puzzle, stamped by the spawner so the same level always presents the same boards.
        public int hackSeed;

        // True once this objective's hack has been won; it can't be hacked twice.
        private bool Hacked { get; set; }

        private Vector2Int _cell;

        // The level this objective sits in, for the kinds to act on once they are hacked.
        protected WorldContext World { get; private set; }

        // Called by the spawner after Instantiate, so the use key can find us on the grid.
        public void Init(WorldContext world)
        {
            World = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        // Used by the player to start the hack; completion arrives via CompleteHack once the minigame validates a circuit.
        public void OnUsed(Actor user)
        {
            if (user is not PlayerActor || Hacked) return;
            HackRequested?.Invoke(this);
        }

        // Called by the minigame when the circuit activates.
        public void CompleteHack()
        {
            Hacked = true;
            OnHacked();
        }

        // What winning this objective's hack is worth.
        protected abstract void OnHacked();

        private void OnDestroy()
        {
            if (World != null && World.Occupancy.At(_cell) == gameObject) World.Occupancy.Remove(_cell);
        }
    }
}
