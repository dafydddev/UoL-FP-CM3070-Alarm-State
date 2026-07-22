using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player
{
    // What the player is carrying, grouped by kind, and the use slot that spends one.
    // The selected kind is the item the use key acts on.
    // PlayerActor owns the use key and calls TryUse, the inventory does not read input itself.
    public class PlayerInventory : MonoBehaviour
    {
        // Fires with the kind now shown in the use slot, or null once the slot is empty.
        // Static so the scene's HUD can listen without a reference to the spawned player.
        public static event Action<ItemKind?> OnSlotChanged;

        private readonly List<IInventoryItem> _items = new();

        // The kind the use key acts on, or null before anything has filled the slot.
        public ItemKind? Selected { get; private set; }

        // Announce the empty slot on spawn, the way PlayerHealth announces its hearts, so the HUD starts clean.
        private void Awake() => OnSlotChanged?.Invoke(Selected);

        // How many of a kind the player is holding, for the inventory screen's slots.
        public int CountOf(ItemKind kind)
        {
            return _items.Count(item => item.Kind == kind);
        }

        // Picks an item up. The first item into an empty inventory fills the use slot.
        public void Collect(IInventoryItem item)
        {
            var wasEmpty = _items.Count == 0;
            _items.Add(item);
            if (!wasEmpty) return;
            Selected = item.Kind;
            OnSlotChanged?.Invoke(Selected);
        }

        // Puts a kind into the use slot; called by the inventory screen.
        public void Select(ItemKind kind)
        {
            Selected = kind;
            OnSlotChanged?.Invoke(Selected);
        }

        // Uses one item of the selected kind, spending it only if it managed to act.
        // Spending the last of the kind empties the slot.
        public bool TryUse(Vector2Int userCell)
        {
            if (Selected == null) return false;
            var kind = Selected.Value;
            for (var i = _items.Count - 1; i >= 0; i--) // the most recently collected of its kind first
            {
                if (_items[i].Kind != kind) continue;
                if (!_items[i].Use(userCell)) return false; // item refused; keep it in hand
                _items.RemoveAt(i);
                if (CountOf(kind) != 0) return true; // that was the last of it; empty the slot
                Selected = null;
                OnSlotChanged?.Invoke(Selected);
                return true;
            }

            return false; // nothing of the selected kind to spend
        }
    }
}