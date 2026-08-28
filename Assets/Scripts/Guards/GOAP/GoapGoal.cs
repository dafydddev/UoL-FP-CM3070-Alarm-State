namespace Guards.Goap
{
    // A candidate goal: the world state it wants to bring about, how important it is, and the (partial) state under which it applies.
    // Both conditions are plain WorldStates, so relevance and desire live in one representation and cannot drift apart.
    public sealed class GoapGoal
    {
        public string Name { get; }
        public int Priority { get; } // higher wins when several goals are relevant
        public WorldState RelevantWhen { get; } // empty = always relevant
        public WorldState Desired { get; }

        public GoapGoal(string name, int priority, WorldState relevantWhen, WorldState desired)
        {
            Name = name;
            Priority = priority;
            RelevantWhen = relevantWhen;
            Desired = desired;
        }
    }
}