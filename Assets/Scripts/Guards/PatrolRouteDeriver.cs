using System.Collections.Generic;
using System.Linq;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;

namespace Guards
{
    // Derives a guard's patrol route from the room graph.
    // The guard's own post plus its graph-adjacent rooms, as room-centre cells.
    // Patrols therefore stay local and follow the facility's actual connectivity rather than arbitrary points.
    public static class PatrolRouteDeriver
    {
        public static List<Vector2Int> Derive(string homeRoomId, RoomGraph graph, IReadOnlyDictionary<string, RoomRect> rects)
        {
            var route = new List<Vector2Int>();
            AddRoomCentre(route, rects, homeRoomId);

            foreach (var neighbourId in Neighbours(homeRoomId, graph))
            {
                // Patrols don't wander into exits or hang around the player's spawn.
                var room = graph.GetRoom(neighbourId);
                if (room == null || room.type is RoomType.Exit or RoomType.Entrance) continue;
                AddRoomCentre(route, rects, neighbourId);
            }

            return route;
        }

        private static void AddRoomCentre(List<Vector2Int> route, IReadOnlyDictionary<string, RoomRect> rects, string roomId)
        {
            if (rects.TryGetValue(roomId, out var rect)) route.Add(new Vector2Int(rect.CenterX, rect.CenterY));
        }

        // Undirected neighbours across an unlocked edge: patrols stay this side of locked doors,
        private static IEnumerable<string> Neighbours(string id, RoomGraph graph)
        {
            foreach (var edge in graph.edges.Where(edge => !edge.locked))
            {
                if (edge.fromId == id) yield return edge.toId;
                else if (edge.toId == id) yield return edge.fromId;
            }
        }
    }
}