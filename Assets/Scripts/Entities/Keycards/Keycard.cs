using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities.Keycards
{
    public class Keycard : MonoBehaviour, IEnterHandler
    {
        public string keyId;

        private Vector2Int _cell;

        private void Start()
        {
            _cell = (Vector2Int)World.Instance.Tilemap.WorldToCell(transform.position);
            World.Instance.Place(_cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerKeycardInventory inventory)) return;
            inventory.Collect(keyId);
            World.Instance.Remove(_cell);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (World.Instance && World.Instance.OccupantAt(_cell) == gameObject)
                World.Instance.Remove(_cell);
        }
    }
}