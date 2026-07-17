using System.Collections.Generic;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // The original layout: the root-to-primary path laid left-to-right as a straight spine,
    // with each spine room's off-spine subtrees hung alternately above and below.
    // Subtrees are placed with backtracking, so crowded branches reroute instead of stacking two rooms on the same cell.
    // Using a greedy overlap is used as a last resort when no arrangement exists at all.
    internal static class SpineLayout
    {
        // The step budget is a worst-case time cap, not a tuning target.
        // Sized per room; the factor reproduces the values a 32,000-level sweep validated (zero degraded layouts) at typical sizes.
        private const int JointStepsPerRoom = 5000; // whole-level search: ~150k tries at ~30 rooms
        private const int RescueStepsPerRoom = 400; // per-subtree rescue
        private const int JointRetries = 4; // seeded reshuffles
        private const int SubtreeRetries = 8; // rescue reshuffles are cheap, so a few more than the joint search

        // Lays the level out; false means at least one subtree needed the greedy last resort and rooms may be stacked
        public static bool Place(RoomGraph graph, string root,
            Dictionary<string, string> parent,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell,
            List<(string a, string b)> connections)
        {
            // The "spine" is the path from root to the primary objective room, laid out in a straight line.
            var primary = graph.rooms.Find(r => r.type == RoomType.PrimaryObjectiveRoom)?.id;
            var spine = PathTo(primary, parent, root) ?? new List<string> { root };
            var onSpine = new HashSet<string>(spine);
            var byNeed = SubtreePlacer.BySubtreeSize(children); // biggest branches placed first
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Tiles, graph.level));
            var state = new SubtreePlacer.State { Cell = cell, Connections = connections, Rng = null };
            // Lay the spine left-to-right along y = 0, connecting each room to the previous.
            for (var i = 0; i < spine.Count; i++)
                state.Add(spine[i], new Vector2Int(i, 0), i > 0 ? spine[i - 1] : null);

            // Gather each spine room's off-spine children as hang points, alternating above/below.
            var hangs = new List<(string parentId, string node, int dirY)>();
            foreach (var s in spine)
            {
                if (!byNeed.TryGetValue(s, out var kids)) continue;
                var side = 1;
                foreach (var child in kids)
                {
                    if (onSpine.Contains(child)) continue;
                    hangs.Add((s, child, side));
                    side = -side; // alternate sides for the next child
                }
            }

            // Place all subtrees as one joint search, so a subtree can yield a cell that a later one needs.
            var jointBudget = JointStepsPerRoom * graph.rooms.Count;
            state.Steps = jointBudget;
            if (SubtreePlacer.TryPlaceForest(hangs, byNeed, state)) return true;
            state.Rng = rng;
            for (var retry = 0; retry < JointRetries; retry++)
            {
                state.Steps = jointBudget;
                if (SubtreePlacer.TryPlaceForest(hangs, byNeed, state)) return true;
            }

            // Joint search found nothing: rescue subtrees one at a time so a single impossible branch doesn't degrade the rest of the level.
            var rescueBudget = RescueStepsPerRoom * graph.rooms.Count;
            var clean = true;
            foreach (var (parentId, node, side) in hangs)
                clean &= Hang(parentId, node, side, byNeed, state, rng, rescueBudget);
            return clean;
        }

        // Hangs one subtree off a spine room: the preferred side first, the other side if the preferred one can't fit it,
        // then seeded reshuffles, and only then the pre-backtracking greedy placement, so a level always generates.
        // False reports that the greedy last resort was needed.
        private static bool Hang(string parentId, string node, int side,
            Dictionary<string, List<string>> children, SubtreePlacer.State state, System.Random rng, int budget)
        {
            state.Rng = null;
            state.Steps = budget;
            if (SubtreePlacer.TryPlace(parentId, node, side, children, state)) return true;
            state.Steps = budget;
            if (SubtreePlacer.TryPlace(parentId, node, -side, children, state)) return true;

            state.Rng = rng; // crowded: let shuffled candidate orders explore other shapes
            for (var retry = 0; retry < SubtreeRetries; retry++)
            {
                state.Steps = budget;
                if (!SubtreePlacer.TryPlace(parentId, node, side, children, state)) continue;
                state.Rng = null;
                return true;
            }

            state.Rng = null; // later subtrees still get the deterministic preference pass
            GreedyPlaceSubtree(parentId, node, side, children, state);
            return false;
        }

        // Last resort: the original greedy placement, kept for the (measured ~never) case where no overlap-free arrangement exists within budget.
        private static void GreedyPlaceSubtree(string parentId, string node, int dirY,
            Dictionary<string, List<string>> children, SubtreePlacer.State state)
        {
            var pos = FindFree(state.Cell[parentId], dirY, state.Used);
            state.Add(node, pos, parentId);
            if (!children.TryGetValue(node, out var kids)) return;
            foreach (var child in kids)
                GreedyPlaceSubtree(node, child, dirY, children, state);
        }

        // Finds the first free neighbouring cell, preferring the branch direction, then sideways.
        private static Vector2Int FindFree(Vector2Int from, int dirY, HashSet<Vector2Int> used)
        {
            var candidates = new[]
            {
                from + new Vector2Int(0, dirY), // preferred: away from the spine
                from + new Vector2Int(1, 0),
                from + new Vector2Int(-1, 0),
                from + new Vector2Int(0, -dirY),
            };
            foreach (var c in candidates)
            {
                if (!used.Contains(c)) return c;
            }

            // All neighbours taken — fall back to the preferred cell (may overlap).
            return from + new Vector2Int(0, dirY);
        }

        // Walks parent links from target back to root and returns the root-to-target path, or null.
        private static List<string> PathTo(string target, Dictionary<string, string> parent, string root)
        {
            if (target == null) return null;
            var path = new List<string>();
            var cur = target;
            var guard = 0;
            while (cur != null)
            {
                path.Add(cur);
                if (cur == root) break;
                if (!parent.TryGetValue(cur, out cur)) cur = null;
                // A real path can't visit more rooms than have parents; longer means a cycle.
                if (++guard > parent.Count) break;
            }

            path.Reverse();
            // Only valid if we actually reached the root.
            return path.Count > 0 && path[0] == root ? path : null;
        }
    }
}
