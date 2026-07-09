using System.Collections.Generic;
using System.Linq;
using Entities.Objectives;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    // Spawns an objective pickup/interactable at the centre of every objective room.
    public class ObjectiveSpawner : PropSpawner
    {
        [SerializeField] private GameObject objectivePrefab;

        // Places an objective in each ObjectiveRoom, tagged with that room's id.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            foreach (var room in graph.rooms.Where(r => r.type == RoomType.ObjectiveRoom))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Spawn in the room centre.
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(objectivePrefab, worldPos, Quaternion.identity, transform);
                // Ensure it has an Objective component and link it to its room id (used by the tracker).
                var objective = go.GetComponent<Objective>();
                objective.id = room.id;
                go.name = $"Objective_{room.id}";
            }
        }
    }
}