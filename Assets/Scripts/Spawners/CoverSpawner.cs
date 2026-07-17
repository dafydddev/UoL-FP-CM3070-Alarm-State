using System.Collections.Generic;
using System.Linq;
using Generation;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    // Scatters cover objects around the facility, placing each against a room's interior wall.
    public class CoverSpawner : PropSpawner
    {
        public GameObject coverPrefab;
        public int count = 6;
        private const int WallCount = 4;
        private const int MaxAttempts = 8;

        // Places up to count cover objects in randomly chosen eligible rooms.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            // Seed from the graph so cover placement is repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Cover, graph.level));

            // Eligible rooms: corridors, objective rooms, and guard posts that have a rectangle.
            var candidates = graph.rooms
                .Where(r => r.type.IsObjective() || r.type is RoomType.Corridor or RoomType.GuardPost)
                .Where(r => rects.ContainsKey(r.id))
                .ToList();

            // Fisher yates shuffle so the chosen rooms vary.
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            // Spawn one cover object per room, up to the requested count.
            var n = Mathf.Min(count, candidates.Count);
            for (var i = 0; i < n; i++)
            {
                var rect = rects[candidates[i].id];
                var cell = WallCell(rect, rng);
                var pos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(coverPrefab, pos, Quaternion.identity, transform);
                go.name = $"Cover_{candidates[i].id}";
            }
        }

        // Picks a random cell along one of the room's four interior walls, avoiding the midpoint (where the door is).
        // Cases: 0 bottom wall, 1 top wall, 2 left wall, 3 right wall.
        private static Vector2Int WallCell(RoomRect r, System.Random rng)
        {
            return rng.Next(WallCount) switch
            {
                0 => new Vector2Int(AvoidMid(rng, r.X + 1, r.Right - 2, r.CenterX), r.Y + 1),
                1 => new Vector2Int(AvoidMid(rng, r.X + 1, r.Right - 2, r.CenterX), r.Bottom - 2),
                2 => new Vector2Int(r.X + 1, AvoidMid(rng, r.Y + 1, r.Bottom - 2, r.CenterY)),
                _ => new Vector2Int(r.Right - 2, AvoidMid(rng, r.Y + 1, r.Bottom - 2, r.CenterY)),
            };
        }

        // Returns a random value in [lo, hi] that isn't `mid`, so cover never lands on the doorway.
        // Tries a few times, then falls back to an endpoint.
        private static int AvoidMid(System.Random rng, int lo, int hi, int mid)
        {
            if (hi <= lo) return lo;
            for (var k = 0; k < MaxAttempts; k++)
            {
                var v = rng.Next(lo, hi + 1);
                if (v != mid) return v;
            }
            return lo == mid ? hi : lo;
        }
    }
}