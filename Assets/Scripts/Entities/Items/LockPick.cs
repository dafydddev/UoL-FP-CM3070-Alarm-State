using Generation.Cells;
using Player;
using Settings;
using Simulation;
using UnityEngine;

namespace Entities.Items
{
    // A lock pick. Picked up into the inventory, then spent to open locked doors, one use per door.
    public class LockPick : MonoBehaviour, IEnterHandler, IInventoryItem, ISpawnedEntity
    {
        // The cells a pick can reach from where the user stands, matching how the player steps.
        private static readonly Vector2Int[] Reach =
            { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public string lockPickId;

        [SerializeField, Min(1)] private int uses = 1;

        // How many doors it opens instead once the kind's upgrade has been bought.
        [SerializeField, Min(1)] private int upgradedUses = 2;

        [SerializeField, Min(0)] private int cashInValue = 75;

        public string ItemId => lockPickId;

        public ItemKind Kind => ItemKind.LockPick;

        // Used up once the last of its uses has opened a door.
        public bool IsSpent => _remaining <= 0;

        public int CashInValue => cashInValue;

        private Vector2Int _cell;
        private WorldContext _world;

        // The doors this pick has left in it, taken from the upgrade when it comes into the world.
        private int _remaining;

        private void Awake() => _remaining = UpgradeSettings.IsUpgraded(Kind) ? upgradedUses : uses;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        public void Bind(WorldContext world) => _world = world;

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerInventory inventory)) return;
            inventory.Collect(this);
            _world.Occupancy.Remove(_cell);
            gameObject.SetActive(false);
        }

        // Opens the first locked door standing next to the user, spending one of the pick's uses.
        // Returns false when there is no such door, which leaves it in the inventory for a door that has one.
        public bool Use(Vector2Int userCell)
        {
            foreach (var direction in Reach)
            {
                var occupant = _world.Occupancy.At(userCell + direction);
                if (!occupant || !occupant.TryGetComponent(out LockedDoor door) || !door.Unlock()) continue;
                _remaining--;
                if (IsSpent) Destroy(gameObject);
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