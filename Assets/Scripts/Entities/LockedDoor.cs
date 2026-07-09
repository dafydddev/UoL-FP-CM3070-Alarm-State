using Entities.Keycards;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    public class LockedDoor : MonoBehaviour, IEntryBlocker, IEnterHandler
    {
        public string keyId;
        [SerializeField] private Sprite openSprite;

        private SpriteRenderer _sprite;
        private Vector2Int _cell;
        private bool _open;
        private WorldContext _world;

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
            return !(mover && mover.TryGetComponent(out PlayerInventory inventory) && inventory.HasKey(keyId));
        }

        public void OnEntered(Actor mover)
        {
            if (_open) return;
            _open = true;
            if (_sprite && openSprite) _sprite.sprite = openSprite;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject)
                _world.Occupancy.Remove(_cell);
        }
    }
}
