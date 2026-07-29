using Generation.Cells;
using Player;
using Settings;
using Simulation;
using UnityEngine;

namespace Entities.Items
{
    // A health item. Picked up into the inventory, then used to restore hearts, never past the player's maximum.
    public class HealthPickup : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        public string healthPickupId;

        [SerializeField, Min(1)] private int hearts = 1;

        // What it restores instead once the kind's upgrade has been bought.
        [SerializeField, Min(1)] private int upgradedHearts = 2;

        [SerializeField, Min(0)] private int cashInValue = 125;

        public string ItemId => healthPickupId;

        public ItemKind Kind => ItemKind.HealthPack;

        // Restoring hearts uses the whole pack up.
        public bool IsSpent => true;

        public int CashInValue => cashInValue;

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

        public void Bind(WorldContext world) => world.Player.TryGetComponent(out _player);

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerInventory inventory)) return;
            mover.TryGetComponent(out _player);
            inventory.Collect(this);
            _world.Occupancy.Remove(_cell);
            gameObject.SetActive(false);
        }

        // Using it restores its hearts and spends it.
        // Returns false on full hearts, which keeps it in the inventory until it is worth spending.
        public bool Use(Vector2Int _)
        {
            if (!_player || !_player.Heal(UpgradeSettings.IsUpgraded(Kind) ? upgradedHearts : hearts)) return false;
            Destroy(gameObject);
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}
