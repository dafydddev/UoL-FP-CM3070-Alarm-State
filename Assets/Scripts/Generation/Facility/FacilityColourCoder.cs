using System.Collections.Generic;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Facility
{
    // Tints each room's floor tiles a flat colour based on its role.
    public class FacilityColourCoder : MonoBehaviour
    {
        // Colours every room in the graph that has both a known rectangle and a known role.
        public static void Apply(Tilemap tilemap, RoomGraph graph, Dictionary<string, RoomRect> rects)
        {
            foreach (var room in graph.rooms)
            {
                // Skip rooms we have no rectangle for, or whose role has no assigned colour.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                if (!RoomColour.TryFor(room.type, out var colour)) continue;
                TintRect(tilemap, rect, colour);
            }
        }

        // Tints every existing tile inside the given rectangle the chosen colour.
        private static void TintRect(Tilemap tilemap, RoomRect rect, Color colour)
        {
            // Walk every cell in the rectangle (Right/Bottom are exclusive bounds).
            for (var x = rect.X; x < rect.Right; x++)
            {
                for (var y = rect.Y; y < rect.Bottom; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(pos)) continue;

                    // Clear LockColor so SetColor takes effect, then tint.
                    tilemap.SetTileFlags(pos, TileFlags.None);
                    tilemap.SetColor(pos, colour);
                }
            }
        }
    }
}