using System;
using System.Collections.Generic;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // What the player is carrying, and the use action that spends it: pick an item up, use it, the item does the rest.
    // Keycards do not go through this loop, so PlayerKeyring holds them instead.
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private InputActionReference useAction;

        public event Action<string> OnItemCollected;

        private readonly List<IInventoryItem> _items = new();

        private void OnEnable()
        {
            useAction.action.performed += OnUse;
            useAction.action.Enable();
        }

        private void OnDisable()
        {
            useAction.action.performed -= OnUse;
            useAction.action.Disable();
        }

        public void Collect(IInventoryItem item)
        {
            _items.Add(item);
            OnItemCollected?.Invoke(item.ItemId);
        }

        // Uses the most recently collected item, spending it only if it managed to act.
        private void OnUse(InputAction.CallbackContext _)
        {
            if (GameLock.Locked) return; // input arrives outside the tick loop, so it checks the lock itself
            if (_items.Count == 0) return;
            var last = _items.Count - 1;
            if (!_items[last].Use(transform.position)) return; // item refused; keep it in hand
            _items.RemoveAt(last);
        }
    }
}
