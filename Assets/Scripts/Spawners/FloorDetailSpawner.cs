using System.Collections.Generic;
using Generation;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    // Scatters floor details across the whole facility. 
    // Spacing is a minimum rather than an average and stops them settling into a lattice.
    public class FloorDetailSpawner : PropSpawner
    {
        [SerializeField] private GameObject[] detailPrefabs;

        // Cells between details.
        [SerializeField, Min(2f)] private float spacing = 8f;

        // How far a detail is darkened below its room's colour. Nearer 1 disappears into the floor.
        [SerializeField, Range(0f, 1f)] private float shade = 0.4f;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            if (detailPrefabs is not { Length: > 0 } || rects.Count == 0) return;

            // Seed from the graph so the scatter is repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Detail, graph.level));

            // Each room paired with the colour its details take, so placing a sample is one lookup.
            var rooms = new List<(RoomRect rect, Color colour, bool tinted)>();
            foreach (var room in graph.rooms)
            {
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                var tinted = RoomColour.TryFor(room.type, out var colour);
                rooms.Add((rect, colour, tinted));
            }

            if (rooms.Count == 0) return;

            // One pass over the facility's full extent, so the spacing holds across room boundaries.
            Bounds(rooms, out var min, out var max);
            var cells = PoissonDisk.Sample(min.x, min.y, max.x, max.y, spacing, rng);

            foreach (var cell in cells)
            {
                var index = RoomAt(rooms, cell);
                if (index < 0) continue; // walls, doorways, and the void between rooms

                var (_, colour, tinted) = rooms[index];
                var prefab = detailPrefabs[rng.Next(detailPrefabs.Length)];
                var pos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                go.name = $"Detail_{cell.x}_{cell.y}";

                // Details take their room's colour like every other prop
                if (!tinted) continue;
                var sprite = go.GetComponentInChildren<SpriteRenderer>();
                if (sprite) sprite.color = new Color(colour.r * shade, colour.g * shade, colour.b * shade, sprite.color.a);
            }
        }

        // The facility's tile extent: the union of every room rectangle.
        private static void Bounds(List<(RoomRect rect, Color colour, bool tinted)> rooms,
            out Vector2Int min, out Vector2Int max)
        {
            min = new Vector2Int(int.MaxValue, int.MaxValue);
            max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var (r, _, _) in rooms)
            {
                min = Vector2Int.Min(min, new Vector2Int(r.X, r.Y));
                max = Vector2Int.Max(max, new Vector2Int(r.Right - 1, r.Bottom - 1));
            }
        }

        // The room whose interior holds this cell, or -1.
        // The wall ring is excluded, so a detail never lands on a wall or in a doorway.
        private static int RoomAt(List<(RoomRect rect, Color colour, bool tinted)> rooms, Vector2Int cell)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i].rect;
                if (cell.x > r.X && cell.x < r.Right - 1 && cell.y > r.Y && cell.y < r.Bottom - 1) return i;
            }
            
            return -1;
        }
    }
}
