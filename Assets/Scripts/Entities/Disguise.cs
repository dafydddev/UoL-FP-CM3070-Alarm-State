using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A disguise. Picked up into the inventory, then worn for a stretch of time.
    // Guards cannot spot the player unless the guard sees the player activating the disguise.
    public class Disguise : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        public string disguiseId;

        [SerializeField, Min(0f)] private float durationSeconds = 10f;

        public string ItemId => disguiseId;

        private Vector2Int _cell;
        private WorldContext _world;

        // The player who picked it up, kept so using the item can start their disguise.
        private PlayerDisguise _player;

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

        // Putting the disguise on starts its clock and spends it.
        // Returns false if the player has no PlayerDisguise to run that clock, keeping it in the inventory.
        public bool Use(Vector3 _)
        {
            if (!_player) return false;
            _player.Wear(durationSeconds);
            Destroy(gameObject);
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}
