using Generation.Tiles;
using UnityEngine;

namespace Generation.Facility
{
    // The generated grid of terrain tiles, queried by cell.
    public class FacilityGrid
    {
        private readonly TileDefinition[,] _tiles;

        public FacilityGrid(TileDefinition[,] tiles) => _tiles = tiles;

        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);

        // Null off the grid and on the void between rooms alike, which the entry rules read as blocking.
        public TileDefinition At(Vector2Int cell) =>
            cell is { x: >= 0, y: >= 0 } &&
            cell.x < Width && cell.y < Height ? _tiles[cell.x, cell.y] : null;
    }
}