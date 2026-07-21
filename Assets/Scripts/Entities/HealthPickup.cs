using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A health item. Picked up into the inventory, then used to restore one heart, never past the player's maximum.
    public class HealthPickup : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        public string healthPickupId;

        public string ItemId => healthPickupId;

        private Vector2Int _cell;
        private WorldContext _world;

        // The player who picked it up, kept so using the item can heal them.
        private PlayerHealth _player;

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
            mover.TryGetComponent(out _player);
            inventory.Collect(this);
            _world.Occupancy.Remove(_cell);
            gameObject.SetActive(false);
        }

        // Using it restores a heart and spends it.
        // Returns false on full hearts, which keeps it in the inventory until it is worth spending.
        public bool Use(Vector3 _)
        {
            if (!_player || !_player.Heal()) return false;
            Destroy(gameObject);
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}
