using System.Collections.Generic;
using System.Linq;
using Entities.Objectives;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns an objective interactable at the centre of every objective room and wires it
    // into the world so the player can use it and, for the primary one, unlock the exit.
    public class ObjectiveSpawner : EntitySpawner
    {
        [SerializeField] private GameObject objectivePrefab;

        // Places an objective in each objective room, tinted with that room's role colour.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            foreach (var room in graph.rooms.Where(r => r.type.IsObjective()))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Spawn in the room centre.
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(objectivePrefab, worldPos, Quaternion.identity, transform);
                // Link it to its room id and flag the primary, then wire it into the sim.
                var objective = go.GetComponent<Objective>();
                objective.id = room.id;
                objective.isPrimary = room.type == RoomType.PrimaryObjectiveRoom;
                objective.Init(world);
                // Tint to the room's role colour so the primary objective stands out.
                var spriteRend = go.GetComponentInChildren<SpriteRenderer>();
                if (spriteRend && RoomColour.TryFor(room.type, out var colour))
                    spriteRend.color = colour;
                go.name = $"Objective_{room.id}";
            }
        }
    }
}
