using System.Collections.Generic;
using System.Linq;
using Entities.Objectives;
using Generation;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns an objective interactable at the centre of every objective room.
    // Wires it into the world so the player can use it and (for the primary one) unlock the exit.
    public class ObjectiveSpawner : EntitySpawner
    {
        [SerializeField] private GameObject primaryObjectivePrefab;
        [SerializeField] private GameObject secondaryObjectivePrefab;

        // Places an objective in each objective room, tinted with that room's role colour.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Seed each objective's hacking puzzle from the graph so boards are repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Hacking, graph.level));

            // Rewards roll on their own stream, so retuning the puzzles never reshuffles the drops.
            var dropRng = new System.Random(Seeds.For(graph.seed, Seeds.Drops, graph.level));

            foreach (var room in graph.rooms.Where(r => r.type.IsObjective()))
            {
                // Skip rooms we have no rectangle for.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                // Spawn in the room centre.
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                // Each kind has its own prefab: only the secondary one carries a reward table.
                var prefab = room.type == RoomType.PrimaryObjectiveRoom
                    ? primaryObjectivePrefab
                    : secondaryObjectivePrefab;
                var go = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                // Link it to its room id, then wire it into the sim.
                var objective = go.GetComponent<Objective>();
                objective.id = room.id;
                objective.hackSeed = rng.Next();
                if (objective is SecondaryObjective secondary) secondary.dropSeed = dropRng.Next();
                objective.Init(world);
                // Tint to the room's role colour so the primary objective stands out.
                var spriteRend = go.GetComponentInChildren<SpriteRenderer>();
                if (spriteRend && RoomColour.TryFor(room.type, out var colour)) spriteRend.color = colour;
                go.name = $"Objective_{room.id}";
            }
        }
    }
}
