using System.Collections.Generic;

namespace Guards.Goap
{
    // Finds the cheapest action sequence that takes the world from a start state to one satisfying the goal:
    // uniform-cost search forward over action effects.
    // States are small value types, so the visited set and frontier stay allocation-light.
    public static class GoapPlanner
    {
        // Safety cap; the guard domain needs a handful of expansions at most.
        private const int MaxExpansions = 256;

        private sealed class Node
        {
            public WorldState State;
            public Node Parent;
            public GoapAction Action; // action that produced this state (null at the root)
            public int Cost;
        }

        // Returns the cheapest plan from start to goal, [] if already satisfied, or null.
        public static List<GoapAction> Plan(WorldState start, WorldState goal, IReadOnlyList<GoapAction> actions)
        {
            if (start.Satisfies(goal)) return new List<GoapAction>();

            var frontier = new List<Node> { new() { State = start } };
            var bestCost = new Dictionary<WorldState, int> { [start] = 0 };

            for (var expansions = 0; frontier.Count > 0 && expansions < MaxExpansions; expansions++)
            {
                // Pop the cheapest node. The frontier stays tiny, so a scan beats heap bookkeeping.
                var index = 0;
                for (var i = 1; i < frontier.Count; i++)
                {
                    if (frontier[i].Cost < frontier[index].Cost) index = i;
                }

                var current = frontier[index];
                frontier.RemoveAt(index);

                if (current.State.Satisfies(goal)) return Reconstruct(current);

                foreach (var action in actions)
                {
                    if (!current.State.Satisfies(action.Preconditions)) continue;

                    var next = current.State.Apply(action.Effects);
                    var cost = current.Cost + action.Cost;
                    if (bestCost.TryGetValue(next, out var known) && cost >= known) continue;

                    bestCost[next] = cost;
                    frontier.Add(new Node { State = next, Parent = current, Action = action, Cost = cost });
                }
            }

            return null; // no sequence of actions reaches the goal
        }

        // Walks the parent chain back to the root and reverses it into a start-to-goal plan.
        private static List<GoapAction> Reconstruct(Node node)
        {
            var plan = new List<GoapAction>();
            for (; node.Action != null; node = node.Parent) plan.Add(node.Action);
            plan.Reverse();
            return plan;
        }
    }
}