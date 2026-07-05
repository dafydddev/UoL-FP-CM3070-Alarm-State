using System;
using System.Collections.Generic;
using Generation.Keycards;
using UnityEngine;

namespace Player
{
    // Tracks which keycards the player has collected, so locked doors can check access.
    [RequireComponent(typeof(Collider2D))]
    public class PlayerKeycardInventory : MonoBehaviour
    {
        // Raised when a fresh inventory comes up (e.g. new level) so the HUD can clear stale icons.
        public static event Action OnInventoryReset;

        // Raised whenever a new keycard is picked up
        public static event Action<string> OnKeycardCollected;

        private void Awake() => OnInventoryReset?.Invoke();

        // The set of key ids the player currently holds.
        private readonly HashSet<string> _keys = new();

        // A locked door opens when this returns true for its edge.keyRoomId.
        public bool HasKey(string keyId) => keyId != null && _keys.Contains(keyId);

        // Picks up any keycard the player walks into.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Ignore colliders that aren't keycards.
            var card = other.GetComponent<Keycard>();
            if (card == null) return;
            // Store the key id and tell the HUD which card was collected.
            _keys.Add(card.keyId);
            OnKeycardCollected?.Invoke(card.keyId);
            // Remove the pickup from the world.
            Destroy(card.gameObject);
        }
    }
}