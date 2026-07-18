using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A level exit. Fires Reached when the player steps onto it.
    public class Exit : MonoBehaviour, IEnterHandler
    {
        public static event Action Reached;

        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (mover is not PlayerActor) return;
            // The exit stays locked until the primary objective is completed.
            if (!_world.Mission.PrimaryComplete) return;
            Reached?.Invoke();
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}