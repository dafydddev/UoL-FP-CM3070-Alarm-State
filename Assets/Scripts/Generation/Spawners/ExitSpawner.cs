using System.Collections.Generic;
using System.Linq;
using Generation.Layout;
using Generation.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Spawners
{
    // Spawns an exit prefab at the centre of every room flagged as an Exit room.
    public class ExitSpawner : Spawner
    {
        [SerializeField] private GameObject exitPrefab;

        // Places an exit at the centre of each Exit-role room in the graph.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            foreach (var room in graph.rooms.Where(r => r.type == RoomType.Exit))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Convert the room's centre cell to world space and spawn the exit there.
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(exitPrefab, worldPos, Quaternion.identity, transform);
                go.name = $"Exit_{room.id}";
            }
        }
    }
}