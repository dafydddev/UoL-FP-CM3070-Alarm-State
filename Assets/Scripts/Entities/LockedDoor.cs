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

        private void Start()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite) _sprite.color = KeyColour.For(keyId);
            _cell = (Vector2Int)World.Instance.Tilemap.WorldToCell(transform.position);
            World.Instance.Place(_cell, gameObject);
        }

        public bool BlocksEntry(Actor mover)
        {
            if (_open) return false;
            return !(mover.TryGetComponent(out PlayerKeycardInventory inventory) && inventory.HasKey(keyId));
        }

        public void OnEntered(Actor mover)
        {
            if (_open) return;
            _open = true;
            if (_sprite && openSprite) _sprite.sprite = openSprite;
        }

        private void OnDestroy()
        {
            if (World.Instance && World.Instance.OccupantAt(_cell) == gameObject)
                World.Instance.Remove(_cell);
        }
    }
}