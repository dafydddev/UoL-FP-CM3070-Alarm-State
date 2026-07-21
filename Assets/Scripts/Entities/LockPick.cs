using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A lock pick. Picked up into the inventory, then spent to open one locked door.
    // The inventory drops an item once its use succeeds, which is what makes the pick single use.
    public class LockPick : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        // The cells a pick can reach from where the user stands, matching how the player steps.
        private static readonly Vector2Int[] Reach =
            { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public string lockPickId;

        public string ItemId => lockPickId;

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
            if (!mover.TryGetComponent(out PlayerInventory inventory)) return;
            inventory.Collect(this);
            _world.Occupancy.Remove(_cell);
            gameObject.SetActive(false);
        }

        // Opens the first locked door standing next to the user, spending the pick.
        // Returns false when there is no such door, which leaves it in the inventory for a door that has one.
        public bool Use(Vector3 userPosition)
        {
            var from = (Vector2Int)_world.Tilemap.WorldToCell(userPosition);
            foreach (var direction in Reach)
            {
                var occupant = _world.Occupancy.At(from + direction);
                if (!occupant || !occupant.TryGetComponent(out LockedDoor door) || !door.Unlock()) continue;
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}