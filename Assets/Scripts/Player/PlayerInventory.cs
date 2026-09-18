using System;
using System.Collections.Generic;
using System.Linq;
using Tutorials;
using UnityEngine;

namespace Player
{
    // What the player is carrying, grouped by type, and the use slot that spends one.
    // The selected item type is the item the use key acts on.
    // PlayerActor owns the use key and calls TryUse, the inventory does not read input itself.
    public class PlayerInventory : MonoBehaviour
    {
        // Fires with the item type in the use slot and how many are held, or null and 0 once the slot is empty.
        // Static so the scene's HUD can listen without a reference to the spawned player.
        public static event Action<ItemType?, int> OnSlotChanged;

        // Fires when an item is picked up, but not when a loadout is granted at spawn.
        public static event Action<ItemType> Collected;

        // Fires when an item is used, even if it has uses left.
        public static event Action<ItemType> Used;

        private readonly List<IInventoryItem> _items = new();

        // The item type the use key acts on, or null before anything has filled the slot.
        public ItemType? Selected { get; private set; }

        // The item types held, one entry per unit.
        public IEnumerable<ItemType> Types => _items.Select(item => item.Type);

        // What everything still held is worth cashed in, for a run that ends with items to spare.
        public int CashInValue => _items.Sum(item => item.CashInValue);

        // Announce the empty slot on spawn, the way PlayerHealth announces its hearts, so the HUD starts clean.
        private void Awake() => AnnounceSlot();

        // How many of an item type the player is holding, for the inventory screen's slots.
        public int CountOf(ItemType type)
        {
            return _items.Count(item => item.Type == type);
        }

        // Picks an item up off the floor.
        public void Collect(IInventoryItem item)
        {
            Add(item);
            Tutorial.ShowOnce(item.Type.Topic());
            Collected?.Invoke(item.Type);
        }

        // Fills the inventory from a loadout at spawn. Nothing was picked up, so nothing is announced.
        public void Grant(IInventoryItem item) => Add(item);

        // The first item into an empty inventory fills the use slot. Another of the selected type raises its count.
        private void Add(IInventoryItem item)
        {
            var wasEmpty = _items.Count == 0;
            _items.Add(item);
            if (wasEmpty) Selected = item.Type;
            if (item.Type == Selected) AnnounceSlot();
        }

        // Puts an item into the use slot; called by the inventory screen.
        public void Select(ItemType type)
        {
            Selected = type;
            AnnounceSlot();
        }

        // Uses one item of the selected type, dropping it only once it has acted and been used up.
        // Spending the last of the type empties the slot.
        public bool TryUse(Vector2Int userCell)
        {
            if (Selected == null) return false;
            var type = Selected.Value;
            for (var i = _items.Count - 1; i >= 0; i--) // the most recently collected of its type first
            {
                if (_items[i].Type != type) continue;
                if (!_items[i].Use(userCell)) return false; // item refused; keep it in hand
                Used?.Invoke(type);
                if (!_items[i].IsSpent) return true; // it acted but has uses left; keep it in hand
                _items.RemoveAt(i);
                if (CountOf(type) == 0) Selected = null;
                AnnounceSlot();
                return true;
            }

            return false; // nothing of the selected type to spend
        }

        private void AnnounceSlot() => OnSlotChanged?.Invoke(Selected, Selected.HasValue ? CountOf(Selected.Value) : 0);
    }
}