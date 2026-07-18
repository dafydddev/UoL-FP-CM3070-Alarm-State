using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A hiding spot. While the player stands on this cell, they count as hidden
    // (via PlayerHiding's counter), letting them break the line of sight from guards.
    public class CoverItem : MonoBehaviour, IEnterHandler, IExitHandler
    {
        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate, so EntryRules can find us on the grid.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (mover.TryGetComponent(out PlayerHiding hiding)) hiding.Enter(this);
        }

        public void OnExited(Actor mover)
        {
            if (mover.TryGetComponent(out PlayerHiding hiding)) hiding.Exit(this);
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject)
                _world.Occupancy.Remove(_cell);
        }
    }
}