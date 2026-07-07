using System;
using System.Collections.Generic;

namespace Graphs.Missions
{
    // The kind of mission objective the player is given.
    public enum MissionType
    {
        Assassination,
        Theft,
        Sabotage
    }
    
    // One objective in the generated mission, plus the nodes it depends on.
    [Serializable]
    public class MissionNode
    {
        public string id;
        public string text;
        public string label;
        public NodeType nodeType;
        public List<string> dependencies = new(); // ids of nodes that must complete first
    }

    // A complete generated mission: its type, facility, objective nodes, and the seed used.
    [Serializable]
    public class MissionGraph
    {
        public MissionType type;
        public string facility;
        public List<MissionNode> nodes = new();
        public int seed;
    }
}