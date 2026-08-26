using System.Collections.Generic;
using System.Linq;
using Entities;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns an exit at the centre of every room flagged as an Exit room and wires it into the world
    public class ExitSpawner : EntitySpawner
    {
        [SerializeField] private GameObject exitPrefab;

        // Places an exit at the centre of each Exit-role room in the graph.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            foreach (var room in graph.rooms.Where(r => r.type == RoomType.Exit))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Convert the room's centre cell to world space and spawn the exit there.
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(exitPrefab, worldPos, Quaternion.identity, transform);
                var exit = go.GetComponent<Exit>();
                exit.Init(world);
                go.name = $"Exit_{room.id}";
            }
        }
    }
}