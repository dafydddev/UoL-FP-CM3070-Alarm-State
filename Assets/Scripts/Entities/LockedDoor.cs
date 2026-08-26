using System;
using Entities.Keycards;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A door on a locked edge. The player needs its key to pass. Passing through opens it for good.
    public class LockedDoor : MonoBehaviour, IEntryBlocker, IEnterHandler
    {
        // Fires once per door, whether a key or a pick opened it.
        public static event Action Opened;

        // The room the matching keycard is found in, stamped by the spawner.
        public string keyId;
        [SerializeField] private Sprite openSprite;

        private SpriteRenderer _sprite;
        private Vector2Int _cell;
        private bool _open;
        private WorldContext _world;

        // A closed locked door screens the view behind it; a keyholder opening it clears the way.
        public bool BlocksSight => !_open;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world, int seed)
        {
            _world = world;
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite) _sprite.color = KeyColour.For(keyId, seed);
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        // A null mover is an anonymous query (e.g. the nav debug drawer): treat it as keyless.
        public bool BlocksEntry(Actor mover)
        {
            if (_open) return false;
            if (mover && mover is not PlayerActor) return false; // guards can always pass; the player needs the key
            return !(mover && mover.TryGetComponent(out PlayerKeyring keyring) && keyring.HasKey(keyId));
        }

        // Only the player opens the door for good, a guard passing through leaves it locked.
        public void OnEntered(Actor mover)
        {
            if (mover is not PlayerActor) return;
            Unlock();
        }

        // Opens the door without a key, the way a lock pick does.
        // Returns false if it stood open already, so a single-use pick is not spent for nothing.
        public bool Unlock()
        {
            if (_open) return false;
            _open = true;
            if (_sprite && openSprite) _sprite.sprite = openSprite;
            Opened?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}