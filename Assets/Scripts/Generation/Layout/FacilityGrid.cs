using UnityEngine;

namespace Generation.Layout
{
    // The generated grid of tiles, queried by cell.
    public class FacilityGrid
    {
        private readonly TileDefinition[,] _tiles;

        public FacilityGrid(TileDefinition[,] tiles) => _tiles = tiles;

        private bool InBounds(Vector2Int cell) =>
            cell is { x: >= 0, y: >= 0 } &&
            cell.x < _tiles.GetLength(0) && cell.y < _tiles.GetLength(1);

        public TileDefinition At(Vector2Int cell) => _tiles[cell.x, cell.y];

        public bool IsWalkable(Vector2Int cell) => InBounds(cell) && _tiles[cell.x, cell.y] && _tiles[cell.x, cell.y].Walkable;
    }
}