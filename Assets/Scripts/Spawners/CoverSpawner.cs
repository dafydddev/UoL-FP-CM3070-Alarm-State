using System.Collections.Generic;
using System.Linq;
using Entities;
using Generation;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Scatters cover objects around the facility, placing each against a room's interior wall.
    public class CoverSpawner : EntitySpawner
    {
        public GameObject coverPrefab;
        public int count = 6; // fewest rooms to place cover in
        [Range(0f, 1f)] public float roomFraction = 0.75f; // portion of the eligible rooms to place cover in
        private const int WallCount = 4;
        private const int MaxAttempts = 8;

        // Places one cover object in each of the rooms drawn at random from the eligible pool.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            var tilemap = world.Tilemap;

            // Seed from the graph so cover placement is repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Cover, graph.level));

            // Every room, except exits and pressure rooms.
            var candidates = graph.rooms
                .Where(r => r.type is not (RoomType.Exit or RoomType.PressureRoom))
                .Where(r => rects.ContainsKey(r.id))
                .ToList();

            // Fisher yates shuffle so the chosen rooms vary.
            Shuffle.InPlace(candidates, rng);

            // How many rooms get a cover object: scales with the level, floored at count, capped by the candidates.
            var n = Mathf.Min(Mathf.Max(count, Mathf.CeilToInt(candidates.Count * roomFraction)), candidates.Count);
            for (var i = 0; i < n; i++)
            {
                var rect = rects[candidates[i].id];
                var cell = WallCell(rect, rng);
                var pos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(coverPrefab, pos, Quaternion.identity, transform);
                go.GetComponent<CoverItem>().Init(world);
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

        // Returns a random value in [lo, hi] that isn't the middle (e.g. where the objectives and exits spawn).
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