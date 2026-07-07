using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    // Tracks which keycards the player has collected, so locked doors can check access.
    public class PlayerKeycardInventory : MonoBehaviour
    {
        public static event Action OnInventoryReset;
        public static event Action<string> OnKeycardCollected;

        private readonly HashSet<string> _keys = new();

        private void Awake() => OnInventoryReset?.Invoke();

        public bool HasKey(string keyId) => keyId != null && _keys.Contains(keyId);

        public void Collect(string keyId)
        {
            _keys.Add(keyId);
            OnKeycardCollected?.Invoke(keyId);
        }
    }
}