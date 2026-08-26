using System.Collections.Generic;
using System.Linq;
using Generation.Tiles;
using Graphs.Rooms;
using Guards;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns one guard at every guard-post room, handing each its patrol route derived from the room graph.
    // Guard-post placement (and how often it happens) is decided upstream by the room graph generator via the difficulty profile.
    public class GuardSpawner : EntitySpawner
    {
        [SerializeField] private GameObject guardPrefab;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            foreach (var room in graph.rooms.Where(room => room.type == RoomType.GuardPost))
            {
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(guardPrefab, pos, Quaternion.identity, transform);
                go.name = $"Guard_{room.id}";
                var agent = go.GetComponent<GuardAgent>();
                agent.Init(world, PatrolRouteDeriver.Derive(room.id, graph, rects));
            }
        }
    }
}