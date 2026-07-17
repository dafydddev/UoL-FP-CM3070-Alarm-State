using System.Collections.Generic;
using UnityEngine;

namespace Graphs.Rooms
{
    // Maps a room role to its display colour, shared by every view of a room:
    // the facility tilemap tint, props matching their room, and the graph editor.
    public static class RoomColour
    {
        private static readonly Dictionary<RoomType, Color> Colours = new()
        {
            [RoomType.Entrance] = new Color(0.40f, 0.80f, 0.40f), // green
            [RoomType.Exit] = new Color(0.30f, 0.60f, 1.00f), // blue
            [RoomType.PrimaryObjectiveRoom] = new Color(1.00f, 0.25f, 0.25f), // red — the target
            [RoomType.SecondaryObjectiveRoom] = new Color(1.00f, 0.55f, 0.30f), // orange — optional
            [RoomType.KeycardRoom] = new Color(1.00f, 0.85f, 0.30f), // amber
            [RoomType.GuardPost] = new Color(0.80f, 0.50f, 0.90f), // purple
            [RoomType.Corridor] = new Color(0.75f, 0.75f, 0.75f), // grey
        };

        // False for roles with no assigned colour; callers choose their own fallback.
        public static bool TryFor(RoomType type, out Color colour) => Colours.TryGetValue(type, out colour);
    }
}
