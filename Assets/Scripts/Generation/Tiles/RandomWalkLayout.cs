using System.Collections.Generic;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // Lays rooms out with a seeded self-avoiding random walk: each room lands in a randomly
    // chosen free cell orthogonally adjacent to its parent, so the layout wanders instead of
    // following a straight spine. Placement is a backtracking search, so a dead end retries
    // other cells rather than failing the level. Upholds the same contract the shared door
    // carver relies on — one distinct cell per room, doored pairs always adjacent — and stays
    // deterministic per seed + level via its own Seeds stream.
    internal static class RandomWalkLayout
    {
        // The step budget is a worst-case time cap, not a tuning target: a placement that
        // is going to succeed almost always does so in a tiny fraction of it, so it only
        // bounds generation time on graphs that turn out not to fit the grid (those get
        // relief corridors from the generator). Sized per room; the factor reproduces the
        // values a 32,000-level sweep validated (zero degraded layouts) at typical sizes.
        private const int StepsPerRoom = 1000; // per-attempt cap: ~25k tries at ~25 rooms
        private const int MaxAttempts = 16; // fresh shuffles; costs nothing when placement succeeds early

        // Lays the level out; false means no arrangement was found and nothing is placed —
        // the generator either relieves the graph and retries, or falls back to the spine.
        public static bool Place(RoomGraph graph, string root,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell,
            List<(string a, string b)> connections)
        {
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Tiles, graph.level));
            var byNeed = SubtreePlacer.BySubtreeSize(children); // biggest branches placed first
            var budget = StepsPerRoom * graph.rooms.Count;

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cell.Clear();
                connections.Clear();
                var state = new SubtreePlacer.State
                    { Cell = cell, Connections = connections, Rng = rng, Steps = budget };
                state.Add(root, Vector2Int.zero, null);
                if (SubtreePlacer.TryPlaceAll(root, 0, byNeed, state)) return true;
            }

            return false;
        }
    }
}
