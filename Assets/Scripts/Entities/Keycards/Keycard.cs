using Generation.Cells;
using Player;
using Simulation;
using Tutorials;
using UnityEngine;

namespace Entities.Keycards
{
    // A keycard lying in the world. Walking onto it puts its key on the player's inventory.
    public class Keycard : MonoBehaviour, IEnterHandler
    {
        // The room the card was placed in, which is what the doors it opens are stamped with.
        public string keyId;

        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerKeyring keyring)) return;
            keyring.Collect(keyId);
            _world.Occupancy.Remove(_cell);
            Destroy(gameObject);
            Tutorial.ShowOnce(TutorialTopic.KeycardFound);
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}