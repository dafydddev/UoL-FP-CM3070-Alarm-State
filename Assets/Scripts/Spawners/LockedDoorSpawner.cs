using System.Collections.Generic;
using System.Linq;
using Entities;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns a locked-door prefab on each locked edge of the room graph, tagged with the key required to open it.
    public class LockedDoorSpawner : EntitySpawner
    {
        [SerializeField] private GameObject lockedDoorPrefab;

        // Places a locked door between the two rooms of every locked edge in the graph.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            foreach (var edge in graph.edges.Where(e => e.locked))
            {
                // Skip edges whose endpoints we have no rectangle for.
                if (!rects.TryGetValue(edge.fromId, out var a)) continue;
                if (!rects.TryGetValue(edge.toId, out var b)) continue;

                // Work out the boundary cell between the two rooms and convert it to world space.
                var cell = DoorCell(a, b);
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(lockedDoorPrefab, worldPos, Quaternion.identity, transform);

                // Hand the door component the required key and the world.
                // The key id is the room the card is found in, so it matches the keycard the spawner tints.
                var door = go.GetComponent<LockedDoor>();
                door.keyId = edge.keyRoomId;
                door.Init(world, graph.seed);
                go.name = $"LockedDoor_{edge.keyRoomId}";
            }
        }

        // Picks the cell where the door sits, based on which side room b lies relative to room a.
        private static Vector2Int DoorCell(RoomRect a, RoomRect b)
        {
            if (b.X > a.X) return new Vector2Int(b.X, a.CenterY); // b east of a
            if (b.X < a.X) return new Vector2Int(a.X, a.CenterY); // b west of a
            if (b.Y > a.Y) return new Vector2Int(a.CenterX, b.Y); // b at +y of a
            return new Vector2Int(a.CenterX, a.Y); // b at -y of a
        }
    }
}