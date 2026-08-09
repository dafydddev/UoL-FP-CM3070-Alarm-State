using System.Collections.Generic;
using UnityEngine;

namespace Hacking
{
    // The state of one hacking puzzle: a grid of rotatable pipe tiles plus the two fixed nodes power flows between.
    // Power feeds into the start tile from the west, off the board, and the circuit completes when it can leave the end tile eastward.
    // Validation lives here on the board state, so rendering only ever reads results.
    public class PipeBoard
    {
        private readonly PipeTile[,] _tiles;

        // The west-column cell the feed enters and the east-column cell it must leave.
        public Vector2Int StartCell { get; }
        public Vector2Int EndCell { get; }

        public PipeBoard(PipeTile[,] tiles, Vector2Int startCell, Vector2Int endCell)
        {
            _tiles = tiles;
            StartCell = startCell;
            EndCell = endCell;
        }

        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);

        public PipeTile At(Vector2Int cell) =>
            cell is { x: >= 0, y: >= 0 } &&
            cell.x < Width && cell.y < Height
                ? _tiles[cell.x, cell.y]
                : null;

        // The four sides in clockwise order, for walks over a tile's connections.
        private static readonly PipeDirection[] Sides =
        {
            PipeDirection.North, PipeDirection.East, PipeDirection.South, PipeDirection.West
        };

        // Whether the tile's given side meets an open side of the neighbour beyond it.
        private bool Connected(Vector2Int cell, PipeDirection side)
        {
            var tile = At(cell);
            if (tile == null || (tile.Connections & side) == 0) return false;

            var neighbour = At(cell + side.Offset());
            return neighbour != null && (neighbour.Connections & side.Opposite()) != 0;
        }

        // Breadth-first flood from the start node over connected tiles.
        // True once the flow can leave the end tile eastward,
        // with the cells the charge runs through in order the activation surge plays back exactly this path.
        public bool TryTraceCircuit(out List<Vector2Int> path)
        {
            path = null;

            // The start tile must face the feed before any power enters the board.
            var startTile = At(StartCell);
            if (startTile == null || (startTile.Connections & PipeDirection.West) == 0) return false;

            var visited = new HashSet<Vector2Int> { StartCell };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>(); // best predecessor of each cell
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(StartCell);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == EndCell && (At(current).Connections & PipeDirection.East) != 0)
                {
                    path = Reconstruct(cameFrom, current);
                    return true;
                }

                foreach (var side in Sides)
                {
                    var next = current + side.Offset();
                    if (visited.Contains(next) || !Connected(current, side)) continue;
                    visited.Add(next);
                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            return false; // frontier exhausted without reaching the end node
        }

        // Walks the cameFrom chain back from the end and reverses it into a start-to-end path.
        private static List<Vector2Int> Reconstruct(
            Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out current)) path.Add(current);
            path.Reverse();
            return path;
        }
    }
}