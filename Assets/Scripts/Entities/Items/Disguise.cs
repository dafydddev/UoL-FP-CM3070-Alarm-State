using Generation.Cells;
using Player;
using Settings;
using Simulation;
using UnityEngine;

namespace Entities.Items
{
    // A disguise. Picked up into the inventory, then worn for a stretch of time.
    // Guards cannot spot the player unless the guard sees the player activating the disguise.
    public class Disguise : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        public string disguiseId;

        [SerializeField, Min(0f)] private float durationSeconds = 10f;

        // How long the disguise lasts once the upgrade has been bought.
        [SerializeField, Min(0f)] private float upgradedDurationSeconds = 15f;

        [SerializeField, Min(0)] private int cashInValue = 100;

        public string ItemId => disguiseId;

        public ItemType Type => ItemType.Disguise;

        // Putting the disguise on uses it up; its clock runs on the player, not on the item.
        public bool IsSpent => true;

        public int CashInValue => cashInValue;

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

        // The loadout path: granted at spawn rather than picked up, so the player is taken from the world.
        public void Bind(WorldContext world) => world.Player.TryGetComponent(out _player);

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
        public bool Use(Vector2Int _)
        {
            if (!_player) return false;
            _player.Wear(UpgradeSettings.IsUpgraded(Type) ? upgradedDurationSeconds : durationSeconds);
            Destroy(gameObject);
            return true;
        }

        // A collected item is deactivated rather than destroyed, so the cell is cleared here too.
        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}