using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A level exit. Fires Reached when the player uses it.
    public class Exit : MonoBehaviour, IUseHandler
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

        public bool OnUsed(Actor user)
        {
            if (user is not PlayerActor) return false;
            // The exit stays locked until the primary objective is completed, and seals while the alarm sounds.
            if (!_world.Mission.PrimaryComplete || _world.Alarm.Active) return false;
            Reached?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}