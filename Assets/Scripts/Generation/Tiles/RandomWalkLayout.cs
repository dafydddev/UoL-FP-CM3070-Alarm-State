using System.Collections.Generic;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // Lays rooms out with a seeded self-avoiding random walk.
    // Each room lands in a randomly chosen free cell orthogonally adjacent to its parent, so the layout wanders.
    // Placement is a backtracking search, so a dead end retries other cells rather than failing the level.
    // Deterministic per seed + level via its own Seeds stream.
    internal static class RandomWalkLayout
    {
        private const int StepsPerRoom = 1000; // per-attempt cap: ~25k tries at ~25 rooms
        private const int MaxAttempts = 16; // fresh shuffles, costs nothing when placement succeeds early

        // Lays the level out; false means no arrangement was found and nothing is placed.
        // The generator either relieves the graph and retries, or falls back to the spine.
        public static bool Place(RoomGraph graph, string root,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell,
            List<(string a, string b)> connections)
        {
            // One stream across every attempt, so a retry draws on from where the last left off rather than repeating it.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Tiles, graph.level));
            var byNeed = SubtreePlacer.BySubtreeSize(children); // biggest branches placed first
            var budget = StepsPerRoom * graph.rooms.Count;

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cell.Clear();
                connections.Clear();
                // A fresh budget per attempt, so an exhausted search starts over rather than giving up outright.
                var state = new SubtreePlacer.State
                    { Cell = cell, Connections = connections, Rng = rng, Steps = budget };
                state.Add(root, Vector2Int.zero, null);
                if (SubtreePlacer.TryPlaceAll(root, 0, byNeed, state)) return true;
            }

            return false;
        }
    }
}