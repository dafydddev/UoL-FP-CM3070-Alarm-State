using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pathfinding
{
    // Owns the pathfinder and translates between tile-space (what A* works in) and world-space (what agents move in).
    // The Tilemap is the single source of truth for that conversion, matching every spawner in the project.
    // Built as part of the WorldContext, so it always reflects the current level.
    public class Navigator
    {
        private readonly Tilemap _tilemap;

        public AStarPathfinder Pathfinder { get; }

        public Navigator(Tilemap tilemap, EntryRules entry)
        {
            _tilemap = tilemap;
            Pathfinder = new AStarPathfinder(entry);
        }

        private Vector2Int WorldToCell(Vector3 world) => (Vector2Int)_tilemap.WorldToCell(world);

        public Vector3 CellToWorld(Vector2Int cell) => _tilemap.GetCellCenterWorld((Vector3Int)cell);

        // World-space path for the given mover, ready to feed to an agent. Null if unreachable.
        public List<Vector3> FindWorldPath(Vector3 from, Vector3 to, Actor mover)
        {
            var cells = Pathfinder.FindPath(WorldToCell(from), WorldToCell(to), mover);
            if (cells == null) return null;

            var path = new List<Vector3>(cells.Count);
            path.AddRange(cells.Select(CellToWorld));
            return path;
        }
    }
}
