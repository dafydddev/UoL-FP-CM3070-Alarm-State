using System.Collections.Generic;
using Generation.Layout;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Rooms
{
    // Tints each room's floor tiles a flat colour based on its role, so the
    // generated layout is readable at a glance during testing/debugging.
    public class RoomColourCoder : MonoBehaviour
    {
        // The colour used for each room role. Rooms with a role not listed here are left untinted.
        private static readonly Dictionary<RoomType, Color> RoleColours = new()
        {
            [RoomType.Entrance] = new Color(0.40f, 0.80f, 0.40f), // green
            [RoomType.Exit] = new Color(0.30f, 0.60f, 1.00f), // blue
            [RoomType.ObjectiveRoom] = new Color(1.00f, 0.45f, 0.45f), // red
            [RoomType.KeycardRoom] = new Color(1.00f, 0.85f, 0.30f), // amber
            [RoomType.GuardPost] = new Color(0.80f, 0.50f, 0.90f), // purple
            [RoomType.Corridor] = new Color(0.75f, 0.75f, 0.75f), // grey
        };

        // Colours every room in the graph that has both a known rectangle and a known role.
        public static void Apply(Tilemap tilemap, RoomGraph graph, Dictionary<string, RoomRect> rects)
        {
            foreach (var room in graph.rooms)
            {
                // Skip rooms we have no rectangle for, or whose role has no assigned colour.
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                if (!RoleColours.TryGetValue(room.type, out var colour)) continue;
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