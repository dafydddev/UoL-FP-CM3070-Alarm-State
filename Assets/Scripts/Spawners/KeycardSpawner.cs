using System;
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

        // Fires once per level with the keys actually placed and the seed that colours them.
        // Static so the scene's HUD can rebuild its slots without holding a reference to the spawner.
        public static event Action<IReadOnlyList<string>, int> KeysSpawned;

        // Spawns a keycard in each room that acts as a key source for a locked edge.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Gather the distinct key rooms referenced by locked edges.
            // Distinct, as several locked edges may share the one key room.
            var keyRoomIds = graph.edges
                .Where(e => e.locked && e.keyRoomId != null)
                .Select(e => e.keyRoomId)
                .Distinct();

            // Only the keys that actually reach the floor are announced, so the HUD never shows an unobtainable slot.
            var placed = new List<string>();

            foreach (var keyId in keyRoomIds)
            {
                if (!rects.TryGetValue(keyId, out var rect)) continue;
                var worldPos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(keycardPrefab, worldPos, Quaternion.identity, transform);
                var card = go.GetComponent<Keycard>();
                card.keyId = keyId;
                card.Init(world);
                // Tint the sprite to match the key/door colour.
                var spriteRend = go.GetComponent<SpriteRenderer>();
                if (spriteRend) spriteRend.color = KeyColour.For(keyId, graph.seed);
                go.name = $"Keycard_{keyId}";
                placed.Add(keyId);
            }

            // Fires even when the level locked nothing, so the HUD clears the previous level's slots.
            KeysSpawned?.Invoke(placed, graph.seed);
        }
    }
}