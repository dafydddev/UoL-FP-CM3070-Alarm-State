using System;
using Generation.Keycards;
using Player;
using UnityEngine;

namespace Generation
{
    public class LockedDoor : MonoBehaviour
    {
        // The key required to open this door.
        public string keyId;

        public static event Action OnDoorOpened;
        
        private void Start()
        {
            // Colour the door's sprite to match its key.
            var spriteRend = GetComponentInChildren<SpriteRenderer>();
            if (spriteRend) spriteRend.color = KeyColour.For(keyId);
        }

        // Check on first contact and while contact continues.
        private void OnCollisionEnter2D(Collision2D col) => Resolve(col);
        private void OnCollisionStay2D(Collision2D col) => Resolve(col);

        private void Resolve(Collision2D col)
        {

            // Player passes only while holding the matching key.
            var inventory = col.collider.GetComponent<PlayerKeycardInventory>();
            if (inventory && inventory.HasKey(keyId)) Open();
        }

        private void Ignore(Collider2D otherCol)
        {
            foreach (var mine in GetComponents<Collider2D>())
            {
                Physics2D.IgnoreCollision(mine, otherCol);
            }
        }

        // Open the door: switch off its colliders and hide it.
        private void Open()
        {
            foreach (var col in GetComponents<Collider2D>())
            {
                col.enabled = false;
            }
            gameObject.SetActive(false);
            OnDoorOpened?.Invoke();
        }
    }
}