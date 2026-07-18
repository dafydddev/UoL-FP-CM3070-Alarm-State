using System.Collections.Generic;
using System.Linq;
using Entities.Keycards;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns one keycard per locked-edge key, placing it in its key room and tinting it with that key's colour.
    public class KeycardSpawner : EntitySpawner
    {
        [SerializeField] private GameObject keycardPrefab;

        // Spawns a keycard in each room that acts as a key source for a locked edge.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Gather the distinct key rooms referenced by locked edges.
            var keyRoomIds = graph.edges
                .Where(e => e.locked && e.keyRoomId != null)
                .Select(e => e.keyRoomId)
                .Distinct();

            foreach (var keyId in keyRoomIds)
            {
                // Skip if we have no rectangle for the key room.
                if (!rects.TryGetValue(keyId, out var rect)) continue;

                // Spawn the keycard in the room centre.
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(keycardPrefab, worldPos, Quaternion.identity, transform);

                // Ensure it has a Keycard component carrying the key id.
                var card = go.GetComponent<Keycard>();
                card.keyId = keyId;
                card.Init(world);

                // Tint the sprite to match the key/door colour.
                var spriteRend = go.GetComponent<SpriteRenderer>();
                if (spriteRend) spriteRend.color = KeyColour.For(keyId, graph.seed);
                go.name = $"Keycard_{keyId}";
            }
        }
    }
}