using System;
using System.Collections.Generic;

namespace Generation.Rooms
{
    // The gameplay purpose of a room, which drives colouring, spawning, and layout.
    public enum RoomType
    {
        Entrance,
        ObjectiveRoom,
        KeycardRoom,
        GuardPost,
        Corridor,
        Exit
    }
    
    // A single room in the layout graph, linked back to the mission node it represents.
    [Serializable]
    public class RoomNode
    {
        public string id;
        public RoomType type;
        public string missionNodeId; // mission node this room came from, if any
    }
    
    // A connection between two rooms, optionally locked behind a keycard room.
    [Serializable]
    public class RoomEdge
    {
        public string fromId;
        public string toId;
        public bool locked;
        public string keyRoomId; // the keycard room whose key opens this edge
    }

    // The full room layout: rooms, the edges connecting them, and the seed/difficulty used.
    [Serializable]
    public class RoomGraph
    {
        public List<RoomNode> rooms = new();
        public List<RoomEdge> edges = new();
        public int seed;
        public int level;

        // Finds a room by id, or null if not present.
        public RoomNode GetRoom(string id) => rooms.Find(r => r.id == id);
    }

}