using System.Collections.Generic;
using System.Linq;
using Generation.Keycards;
using Generation.Layout;
using Generation.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Spawners
{
    // Spawns one keycard per locked-edge key, placing it in its key room and tinting it with that key's colour.
    public class KeycardSpawner : MonoBehaviour, ISpawner
    {
        public GameObject keycardPrefab;

        // Spawns a keycard in each room that acts as a key source for a locked edge.
        public void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
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
                var worldPos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(keycardPrefab, worldPos, Quaternion.identity, transform);

                // Ensure it has a Keycard component carrying the key id.
                var card = go.GetComponent<Keycard>() ?? go.AddComponent<Keycard>();
                card.keyId = keyId;

                // Tint the sprite to match the key/door colour.
                var spriteRend = go.GetComponentInChildren<SpriteRenderer>();
                if (spriteRend) spriteRend.color = KeyColour.For(keyId);
                go.name = $"Keycard_{keyId}";
            }
        }

        public void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }
}