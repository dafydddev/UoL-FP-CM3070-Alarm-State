using System.Collections.Generic;
using Entities;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Places one alarm switch in each guard post, on a free floor cell that backs onto a wall.
    // Guard posts sit in front of objective and keycard rooms, so switches cluster where intrusions happen.
    public class AlarmSwitchSpawner : EntitySpawner
    {
        [SerializeField] private GameObject alarmSwitchPrefab;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            foreach (var room in graph.rooms)
            {
                if (room.type != RoomType.GuardPost) continue;
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                if (!TryFindWallCell(world, rect, out var cell)) continue;

                var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(alarmSwitchPrefab, pos, Quaternion.identity, transform);
                go.name = $"AlarmSwitch_{room.id}";
                go.GetComponent<AlarmSwitch>().Init(world);
            }
        }

        // A free interior floor cell that backs onto a wall. Falls back to any free interior cell, then the centre.
        private static bool TryFindWallCell(WorldContext world, RoomRect rect, out Vector2Int cell)
        {
            var centre = new Vector2Int(rect.CenterX, rect.CenterY);
            var fallback = centre;
            var hasFallback = false;

            for (var x = rect.X + 1; x < rect.Right - 1; x++)
            for (var y = rect.Y + 1; y < rect.Bottom - 1; y++)
            {
                var candidate = new Vector2Int(x, y);
                if (candidate == centre || !IsFree(world, candidate)) continue;

                if (NextToWall(world, candidate))
                {
                    cell = candidate;
                    return true;
                }

                if (!hasFallback)
                {
                    fallback = candidate;
                    hasFallback = true;
                }
            }

            cell = fallback;
            return hasFallback || IsFree(world, centre);
        }

        private static bool IsFree(WorldContext world, Vector2Int cell)
        {
            var tile = world.Grid.At(cell);
            return tile && !tile.BlocksEntry(null) && !world.Occupancy.At(cell);
        }

        private static bool NextToWall(WorldContext world, Vector2Int cell) =>
            IsWall(world, cell + Vector2Int.up) || IsWall(world, cell + Vector2Int.down) ||
            IsWall(world, cell + Vector2Int.left) || IsWall(world, cell + Vector2Int.right);

        private static bool IsWall(WorldContext world, Vector2Int cell)
        {
            var tile = world.Grid.At(cell);
            return !tile || tile.BlocksEntry(null);
        }
    }
}
