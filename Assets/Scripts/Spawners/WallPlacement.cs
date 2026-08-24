using Generation.Tiles;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Picks the cell for anything that stands against a room's interior wall.
    // Callers decide which rooms they populate and how far from the doorways their prop belongs.
    public static class WallPlacement
    {
        private const int WallCount = 4;
        private const int MaxAttempts = 16;

        // A random free cell along one of the room's four interior walls, standing the given clearance
        // clear of every doorway. Cases: 0 bottom wall, 1 top wall, 2 left wall, 3 right wall.
        // Falls back to the interior corner, which clears every doorway a room can have by four cells.
        public static Vector2Int Pick(WorldContext world, RoomRect r, System.Random rng, int doorClearance)
        {
            for (var k = 0; k < MaxAttempts; k++)
            {
                var cell = rng.Next(WallCount) switch
                {
                    0 => new Vector2Int(rng.Next(r.X + 1, r.Right - 1), r.Y + 1),
                    1 => new Vector2Int(rng.Next(r.X + 1, r.Right - 1), r.Bottom - 2),
                    2 => new Vector2Int(r.X + 1, rng.Next(r.Y + 1, r.Bottom - 1)),
                    _ => new Vector2Int(r.Right - 2, rng.Next(r.Y + 1, r.Bottom - 1)),
                };

                if (IsFree(world, cell) && ClearOfDoors(world, r, cell, doorClearance)) return cell;
            }

            return new Vector2Int(r.X + 1, r.Y + 1);
        }

        // True when no doorway sits within the clearance. Doors are carved at the midpoint of a shared wall,
        // so scanning the ring for gaps finds the walls that have one and leaves the solid walls open.
        private static bool ClearOfDoors(WorldContext world, RoomRect r, Vector2Int cell, int clearance)
        {
            for (var x = Mathf.Max(cell.x - clearance, r.X); x <= Mathf.Min(cell.x + clearance, r.Right - 1); x++)
            for (var y = Mathf.Max(cell.y - clearance, r.Y); y <= Mathf.Min(cell.y + clearance, r.Bottom - 1); y++)
            {
                if (x != r.X && x != r.Right - 1 && y != r.Y && y != r.Bottom - 1) continue;
                if (!IsWall(world, new Vector2Int(x, y))) return false;
            }

            return true;
        }

        private static bool IsFree(WorldContext world, Vector2Int cell)
        {
            var tile = world.Grid.At(cell);
            return tile && !tile.BlocksEntry(null) && !world.Occupancy.At(cell);
        }

        private static bool IsWall(WorldContext world, Vector2Int cell)
        {
            var tile = world.Grid.At(cell);
            return !tile || tile.BlocksEntry(null);
        }
    }
}
