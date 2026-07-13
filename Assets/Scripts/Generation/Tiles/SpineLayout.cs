using System.Collections.Generic;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // The original layout: the root-to-primary path laid left-to-right as a straight spine,
    // with each spine room's off-spine subtrees hung alternately above and below.
    internal static class SpineLayout
    {
        public static void Place(RoomGraph graph, string root,
            Dictionary<string, string> parent,
            Dictionary<string, List<string>> children,
            Dictionary<string, Vector2Int> cell,
            List<(string a, string b)> connections)
        {
            // The "spine" is the path from root to the primary objective room, laid out in a straight line.
            var primary = graph.rooms
                .Find(r => r.type == RoomType.ObjectiveRoom && r.missionNodeId == "primary")?.id;
            var spine = PathTo(primary, parent, root) ?? new List<string> { root };
            var onSpine = new HashSet<string>(spine);
            var used = new HashSet<Vector2Int>();
            // Lay the spine left-to-right along y = 0, connecting each room to the previous.
            for (var i = 0; i < spine.Count; i++)
            {
                var p = new Vector2Int(i, 0);
                cell[spine[i]] = p;
                used.Add(p);
                if (i > 0) connections.Add((spine[i - 1], spine[i]));
            }

            // Hang each spine room's off-spine children as subtrees, alternating above/below.
            foreach (var s in spine)
            {
                if (!children.TryGetValue(s, out var kids)) continue;
                var side = 1;
                foreach (var child in kids)
                {
                    if (onSpine.Contains(child)) continue;
                    PlaceSubtree(s, child, side, cell, used, connections, children);
                    side = -side; // alternate sides for the next child
                }
            }
        }

        // Recursively places child room (and its descendants) in a free cell next to its parent.
        private static void PlaceSubtree(string parentId, string node, int dirY,
            Dictionary<string, Vector2Int> cell, HashSet<Vector2Int> used,
            List<(string, string)> connections, Dictionary<string, List<string>> children)
        {
            var pos = FindFree(cell[parentId], dirY, used);
            cell[node] = pos;
            used.Add(pos);
            connections.Add((parentId, node));
            if (!children.TryGetValue(node, out var kids)) return;
            foreach (var child in kids)
                PlaceSubtree(node, child, dirY, cell, used, connections, children);
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
                if (++guard > 100000) break; // safety against a malformed/cyclic graph
            }

            path.Reverse();
            // Only valid if we actually reached the root.
            return path.Count > 0 && path[0] == root ? path : null;
        }
    }
}
