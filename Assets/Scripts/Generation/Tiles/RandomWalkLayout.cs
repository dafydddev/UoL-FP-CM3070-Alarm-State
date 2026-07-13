using System.Collections.Generic;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // Lays rooms out with a seeded self-avoiding random walk: each room lands in a randomly
    // chosen free cell orthogonally adjacent to its parent, so the layout wanders instead of
    // following a straight spine. Upholds the same contract the shared door carver relies on —
    // one distinct cell per room, doored pairs always adjacent — and stays deterministic per
    // seed + level via its own Seeds stream.
    internal static class RandomWalkLayout
    {
        private const int MaxAttempts = 32;

        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0),
            new(0, 1), new(0, -1),
        };

        public static void Place(RoomGraph graph, string root,
            Dictionary<string, string> parent,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell,
            List<(string a, string b)> connections)
        {
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Tiles, graph.level));

            // A greedy walk can wall itself in; retry with fresh draws from the same
            // seeded stream, so the outcome is still deterministic per seed + level.
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cell.Clear();
                connections.Clear();
                var used = new HashSet<Vector2Int> { Vector2Int.zero };
                cell[root] = Vector2Int.zero;
                if (TryPlaceChildren(root, children, cell, used, connections, rng)) return;
            }

            // Couldn't fit this graph by wandering — the spine shares the same contract,
            // so the level still generates.
            cell.Clear();
            connections.Clear();
            SpineLayout.Place(graph, root, parent, children, cell, connections);
        }

        // Depth-first over the graph; fails as soon as a room has no free adjacent cell.
        private static bool TryPlaceChildren(string node,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell, HashSet<Vector2Int> used,
            List<(string a, string b)> connections, System.Random rng)
        {
            if (!children.TryGetValue(node, out var kids)) return true;

            foreach (var child in kids)
            {
                // Already placed via another parent (the room graph is a DAG, not a tree):
                // keep the extra edge as a doorway only when the two rooms actually touch.
                if (cell.TryGetValue(child, out var existing))
                {
                    if (IsAdjacent(cell[node], existing)) connections.Add((node, child));
                    continue;
                }

                var pos = RandomFreeNeighbour(cell[node], used, rng);
                if (pos == null) return false;

                cell[child] = pos.Value;
                used.Add(pos.Value);
                connections.Add((node, child));
                if (!TryPlaceChildren(child, children, cell, used, connections, rng)) return false;
            }

            return true;
        }

        // Picks uniformly among the free orthogonal neighbours, or null when boxed in.
        private static Vector2Int? RandomFreeNeighbour(Vector2Int from, HashSet<Vector2Int> used,
            System.Random rng)
        {
            var free = new List<Vector2Int>(4);
            foreach (var direction in Directions)
            {
                if (!used.Contains(from + direction)) free.Add(from + direction);
            }

            return free.Count > 0 ? free[rng.Next(free.Count)] : null;
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }
}