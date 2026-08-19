using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.Objectives
{
    // A mission objective the player completes with the use action while standing on or beside it.
    // Using it opens a minigame; the objective completes once the minigame is done.
    // What that completion is worth is left to the two types: see PrimaryObjective and SecondaryObjective.
    public abstract class Objective : MonoBehaviour, IUseHandler
    {
        // Fires when the player uses an incomplete objective, so the minigame screen can open for it.
        public static event Action<Objective> MiniGameRequested;

        // Fires once an objective's minigame is won, so the HUD can tick its row off. Covers both types.
        public static event Action<Objective> Complete;

        public string id;

        // The mission's wording for this objective, stamped by the spawner for the HUD to show.
        public string text;

        public abstract MiniGameType Game { get; }

        // Seed for this objective's puzzle, stamped by the spawner so the same level always presents the same boards.
        public int miniGameSeed;

        // True once this objective's minigame has been won; it can't be won twice.
        private bool _complete;

        private Vector2Int _cell;

        // The level this objective sits in.
        protected WorldContext World { get; private set; }

        // Called by the spawner after Instantiate, so the use key can find us on the grid.
        public void Init(WorldContext world)
        {
            World = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        // Used by the player to start the minigame.
        public bool OnUsed(Actor user)
        {
            if (user is not PlayerActor || _complete) return false;
            MiniGameRequested?.Invoke(this);
            return true;
        }

        // Called by the minigame when the game has been completed.
        public void CompleteMiniGame()
        {
            _complete = true;
            OnWon(); // pay out first, so the world is settled by the time the HUD hears about it
            Complete?.Invoke(this);
        }

        // What winning this objective's minigame is worth.
        protected abstract void OnWon();

        private void OnDestroy()
        {
            if (World != null && World.Occupancy.At(_cell) == gameObject) World.Occupancy.Remove(_cell);
        }
    }
}
