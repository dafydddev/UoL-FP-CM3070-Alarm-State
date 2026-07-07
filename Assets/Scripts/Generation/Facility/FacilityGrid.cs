using Generation.Tiles;
using UnityEngine;

namespace Generation.Facility
{
    // The generated grid of terrain tiles, queried by cell.
    public class FacilityGrid
    {
        private readonly TileDefinition[,] _tiles;

        public FacilityGrid(TileDefinition[,] tiles) => _tiles = tiles;

        public TileDefinition At(Vector2Int cell) =>
            cell is { x: >= 0, y: >= 0 } &&
            cell.x < _tiles.GetLength(0) && cell.y < _tiles.GetLength(1)
                ? _tiles[cell.x, cell.y]
                : null;
    }
}