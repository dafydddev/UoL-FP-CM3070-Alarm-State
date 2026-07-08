using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities.Keycards
{
    public class Keycard : MonoBehaviour, IEnterHandler
    {
        public string keyId;

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
            if (!mover.TryGetComponent(out PlayerKeycardInventory inventory)) return;
            inventory.Collect(keyId);
            _world.Occupancy.Remove(_cell);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject)
                _world.Occupancy.Remove(_cell);
        }
    }
}
