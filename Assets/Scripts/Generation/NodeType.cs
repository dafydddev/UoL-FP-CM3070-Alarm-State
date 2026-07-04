namespace Generation
{
    // The role a node plays in the mission dependency graph.
    public enum NodeType
    {
        Entry, // the starting "infiltrate" node
        Prerequisite, // a step that must be done before the primary objective
        Primary, // the main objective
        Secondary // an optional side objective
    }

    // A single piece of mission text: the objective and its short HUD label.
    [System.Serializable]
    public struct NodeData
    {
        public string text;
        public string label;

        public NodeData(string text, string label)
        {
            this.text = text;
            this.label = label;
        }
    }
}