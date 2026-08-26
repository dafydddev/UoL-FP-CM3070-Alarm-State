using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation.Tiles
{
    // Backtracking room placement shared by the layout strategies.
    // A subtree is flattened to a pre-order list and placed node by node.
    // When a node has no free cell adjacent to its parent, the search steps back and retries earlier nodes.
    // Candidate order is shuffled when an RNG is supplied (random walk) and the fixed away-from-the-spine preference when not (spine).
    internal static class SubtreePlacer
    {
        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0),
            new(0, 1), new(0, -1),
        };

        // Mutable placement state shared across a strategy's whole layout pass.
        internal sealed class State
        {
            public Dictionary<string, Vector2Int> Cell;
            public List<(string a, string b)> Connections;
            public System.Random Rng; // null -> deterministic preference order
            public int Steps; // remaining placement tries before the search gives up

            public readonly HashSet<Vector2Int> Used = new();

            public void Add(string node, Vector2Int pos, string parentId)
            {
                Cell[node] = pos;
                Used.Add(pos);
                if (parentId != null) Connections.Add((parentId, node));
            }
        }

        // Places node and its whole subtree adjacent to parentId.
        // Returns false with the state exactly as it found it.
        public static bool TryPlace(string parentId, string node, int dirY,
            Dictionary<string, List<string>> children, State state)
        {
            return TryPlaceForest(new List<(string, string, int)> { (parentId, node, dirY) }, children, state);
        }

        // Places every child subtree of root as one joint search,
        // so siblings can trade space with each other instead of failing independently.
        public static bool TryPlaceAll(string root, int dirY,
            Dictionary<string, List<string>> children, State state)
        {
            var roots = new List<(string, string, int)>();
            if (children.TryGetValue(root, out var kids))
            {
                roots.AddRange(kids.Select(k => (root, k, dirY)));
            }

            return TryPlaceForest(roots, children, state);
        }

        // Places several subtrees (each with its own hang point and branch direction) as one joint search,
        // so one subtree can yield a cell that another one needs. Returns false with the state exactly as it found it.
        public static bool TryPlaceForest(List<(string parentId, string node, int dirY)> roots,
            Dictionary<string, List<string>> children, State state)
        {
            // One shared seen set, so a room reached from two of these subtrees is placed once.
            var order = new List<(string node, string parent, int dirY)>();
            var extra = new List<(string a, string b)>();
            var seen = new HashSet<string>();
            foreach (var (parentId, node, dirY) in roots)
            {
                Collect(parentId, node, dirY, children, state, order, extra, seen);
            }

            return Search(order, extra, state);
        }

        // Returns a copy of the child lookup with every list sorted largest subtree first.
        // The most constrained branch claims space while there is still room.
        // Prunes most of the backtracking. Deterministic: stable sort, ties keep graph order.
        public static Dictionary<string, List<string>> BySubtreeSize(Dictionary<string, List<string>> children)
        {
            var sizes = new Dictionary<string, int>();

            var sorted = new Dictionary<string, List<string>>(children.Count);
            foreach (var (id, kids) in children)
            {
                sorted[id] = kids.OrderByDescending(SizeOf).ToList();
            }

            return sorted;

            int SizeOf(string id)
            {
                if (sizes.TryGetValue(id, out var known)) return known;
                sizes[id] = 1; // guards against a malformed cyclic graph; overwritten below
                var total = 1;
                if (children.TryGetValue(id, out var kids)) total += kids.Sum(SizeOf);
                return sizes[id] = total;
            }
        }

        // Flattens the subtree into pre-order placement entries.
        // Rooms that are already placed, or appear twice (the room graph could be a DAG),
        // become "extra" edges that get a doorway after the search only if the rooms end up touching.
        private static void Collect(string parentId, string node, int dirY, Dictionary<string, List<string>> children,
            State state,
            List<(string node, string parent, int dirY)> order, List<(string a, string b)> extra,
            HashSet<string> seen)
        {
            if (state.Cell.ContainsKey(node) || !seen.Add(node))
            {
                extra.Add((parentId, node));
                return;
            }

            order.Add((node, parentId, dirY));
            if (!children.TryGetValue(node, out var kids)) return;
            foreach (var k in kids)
            {
                Collect(node, k, dirY, children, state, order, extra, seen);
            }
        }

        // The extra edges are read after the search, so they take the cells the backtracking finally settled on.
        private static bool Search(List<(string node, string parent, int dirY)> order, List<(string a, string b)> extra,
            State state)
        {
            if (!PlaceFrom(0, order, state)) return false;
            foreach (var (a, b) in extra)
            {
                if (state.Cell.TryGetValue(a, out var pa) && state.Cell.TryGetValue(b, out var pb) &&
                    IsAdjacent(pa, pb))
                    state.Connections.Add((a, b));
            }

            return true;
        }

        // Depth-first over the pre-order list: place entry i on a free cell adjacent to its parent,
        // recurse for the rest, and undo on failure so entry i - 1 can try its next candidate.
        private static bool PlaceFrom(int i, List<(string node, string parent, int dirY)> order, State state)
        {
            if (i == order.Count) return true;
            var (node, parentId, dirY) = order[i];
            var from = state.Cell[parentId];
            foreach (var d in Order(dirY, state.Rng))
            {
                if (state.Steps-- <= 0) return false;
                var pos = from + d;
                if (state.Used.Contains(pos)) continue;
                state.Add(node, pos, parentId);
                if (PlaceFrom(i + 1, order, state)) return true;
                // The connection Add appended is the last one, as the deeper calls have undone their own.
                state.Connections.RemoveAt(state.Connections.Count - 1);
                state.Used.Remove(pos);
                state.Cell.Remove(node);
                if (state.Steps <= 0) return false; // out of budget: stop rather than try the remaining candidates
            }

            return false;
        }

        // Candidate directions: shuffled for the walk; for the spine, the same fixed order.
        private static Vector2Int[] Order(int dirY, System.Random rng)
        {
            if (rng == null)
                return new[]
                {
                    new Vector2Int(0, dirY),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, -dirY),
                };

            return Shuffle.Copy(Directions, rng);
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }
}