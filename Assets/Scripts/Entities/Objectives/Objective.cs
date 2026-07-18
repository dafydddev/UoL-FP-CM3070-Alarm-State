using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities.Objectives
{
    // A mission objective the player completes with the use key while standing on or beside it.
    // The primary objective gates the level exit: completing it unlocks the way out.
    public class Objective : MonoBehaviour, IUseHandler
    {
        public string id;

        // Set by the spawner. Only the primary objective unlocks the exit.
        public bool isPrimary;

        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate, so the use key can find us on the grid.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        // Completed by the player using it. Marks the mission's primary objective done,
        // which the exit checks before it will let the player through.
        public void OnUsed(Actor user)
        {
            if (user is PlayerActor && isPrimary) _world.Mission.CompletePrimary();
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}