using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities.Objectives
{
    // A mission objective the player completes with the use key while standing on or beside it.
    // Using it opens the pipe hacking minigame; the objective completes once the hack is done.
    // The primary objective gates the level exit: completing it unlocks the way out.
    public class Objective : MonoBehaviour, IUseHandler
    {
        // Fires when the player uses an unhacked objective, so the hacking screen can open for it.
        public static event Action<Objective> HackRequested;

        public string id;

        // Set by the spawner. Only the primary objective unlocks the exit.
        public bool isPrimary;

        // Seed for this objective's hacking puzzle, stamped by the spawner so the same level always presents the same boards.
        public int hackSeed;

        // True once this objective's hack has been won; it can't be hacked twice.
        private bool Hacked { get; set; }

        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate, so the use key can find us on the grid.
        public void Init(WorldContext world)
        {
            _world = world;
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
        // Marks the mission's primary objective done, which the exit checks before it will let the player through.
        public void CompleteHack()
        {
            Hacked = true;
            if (isPrimary) _world.Mission.CompletePrimary();
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}