using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    // The keycards the player has found. A keycard is not an inventory item.
    // It is never used or dropped, it stays on the ring and opens the doors cut to it (see LockedDoor).
    public class PlayerKeyring : MonoBehaviour
    {
        // Fires with the key's id whenever a keycard is picked up.
        // Static so the scene's HUD can listen without a reference to the spawned player.
        public static event Action<string> OnKeycardCollected;

        private readonly HashSet<string> _keys = new();

        public bool HasKey(string keyId) => keyId != null && _keys.Contains(keyId);

        // Fires on every collection, including a key already held, so the HUD refreshes either way.
        public void Collect(string keyId)
        {
            _keys.Add(keyId);
            OnKeycardCollected?.Invoke(keyId);
        }
    }
}