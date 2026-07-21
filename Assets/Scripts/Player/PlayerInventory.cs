using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    // What the player is carrying, and the use that spends it: pick an item up, use it, the item does the rest.
    // Keycards do not go through this loop, so PlayerKeyring holds them instead.
    // PlayerActor owns the use key and calls TryUse; the inventory does not read input itself.
    public class PlayerInventory : MonoBehaviour
    {
        public event Action<string> OnItemCollected;

        private readonly List<IInventoryItem> _items = new();

        public void Collect(IInventoryItem item)
        {
            _items.Add(item);
            OnItemCollected?.Invoke(item.ItemId);
        }

        // Uses the most recently collected item, spending it only if it managed to act.
        public bool TryUse(Vector2Int userCell)
        {
            if (_items.Count == 0) return false;
            var last = _items.Count - 1;
            if (!_items[last].Use(userCell)) return false; // item refused; keep it in hand
            _items.RemoveAt(last);
            return true;
        }
    }
}
