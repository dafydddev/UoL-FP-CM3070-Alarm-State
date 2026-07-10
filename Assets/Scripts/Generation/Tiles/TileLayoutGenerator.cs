using System.Collections.Generic;
using System.Linq;
using Generation.Cells;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // An axis-aligned room rectangle in tile coordinates, with handy derived bounds.
    public readonly struct RoomRect
    {
        public readonly int X, Y;
        private readonly int _w;
        private readonly int _h;
        public int CenterX => X + _w / 2;
        public int CenterY => Y + _h / 2;
        public int Right => X + _w; // exclusive right edge
        public int Bottom => Y + _h; // exclusive top edge

        public RoomRect(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            _w = w;
            _h = h;
        }
    }

    // Turns a mission RoomGraph into a grid of structural roles: lays rooms out on an abstract
    // cell grid (a straight "spine" to the primary objective with side branches), then marks each
    // room as walls + floor and carves doorways between connected rooms.
    public static class TileLayoutGenerator
    {
        private const int RoomW = 12; // room width in tiles
        private const int RoomH = 12; // room height in tiles

        // Convert an abstract cell coord to a tile origin; rooms overlap by 1 tile so walls are shared.
        private static int Ox(int cx) => cx * (RoomW - 1);
        private static int Oy(int cy) => cy * (RoomH - 1);

        // Builds the structural grid and outputs each room's tile rectangle.
        public static CellRole[,] Generate(RoomGraph graph, out Dictionary<string, RoomRect> roomRects)
        {
            // Build parent/child lookups from the graph edges, tracking which rooms have a parent.
            var children = new Dictionary<string, List<string>>();
            var parent = new Dictionary<string, string>();
            var inbound = new HashSet<string>();
            foreach (var e in graph.edges)
            {
                if (!children.TryGetValue(e.fromId, out var list)) children[e.fromId] = list = new List<string>();
                list.Add(e.toId);
                parent[e.toId] = e.fromId;
                inbound.Add(e.toId);
            }

            // The root is the room with no parent (fall back to the first room).
            var root = graph.rooms.Find(r => !inbound.Contains(r.id))?.id ?? graph.rooms[0].id;
            // The "spine" is the path from root to the primary objective room, laid out in a straight line.
            var primary = graph.rooms
                .Find(r => r.type == RoomType.ObjectiveRoom && r.missionNodeId == "primary")?.id;
            var spine = PathTo(primary, parent, root) ?? new List<string> { root };
            var onSpine = new HashSet<string>(spine);
            // Abstract cell positions per room, the cells already taken, and the connections to door.
            var cell = new Dictionary<string, Vector2Int>();
            var used = new HashSet<Vector2Int>();
            var connections = new List<(string a, string b)>();
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

            // Normalise cell coords so the layout starts at (0, 0).
            var minX = cell.Values.Min(c => c.x);
            var minY = cell.Values.Min(c => c.y);
            var pos = cell.ToDictionary(kv => kv.Key, kv => new Vector2Int(kv.Value.x - minX, kv.Value.y - minY));
            // Size the grid to fit all rooms (+1 for the shared outer wall).
            var gridW = Ox(pos.Values.Max(c => c.x)) + RoomW + 1;
            var gridH = Oy(pos.Values.Max(c => c.y)) + RoomH + 1;
            var grid = new CellRole[gridW, gridH];
            // Mark each room: a wall block with a floor interior, and record its rect.
            roomRects = new Dictionary<string, RoomRect>();
            foreach (var (id, c) in pos)
            {
                var rect = new RoomRect(Ox(c.x), Oy(c.y), RoomW, RoomH);
                roomRects[id] = rect;
                FillRect(grid, rect.X, rect.Y, RoomW, RoomH, CellRole.Wall);
                FillRect(grid, rect.X + 1, rect.Y + 1, RoomW - 2, RoomH - 2, CellRole.Floor);
            }

            // Carve a doorway (floor cell in the shared wall) for every connection.
            foreach (var (a, b) in connections)
            {
                var ca = pos[a];
                var cb = pos[b];
                var dx = cb.x - ca.x;
                var dy = cb.y - ca.y;
                // Place the door on the wall facing whichever direction b sits relative to a.
                int doorX, doorY;
                if (dx == 1) // b to the east
                {
                    doorX = Ox(cb.x);
                    doorY = Oy(ca.y) + RoomH / 2;
                }
                else if (dx == -1) // b to the west
                {
                    doorX = Ox(ca.x);
                    doorY = Oy(ca.y) + RoomH / 2;
                }
                else if (dy == 1) // b above
                {
                    doorX = Ox(ca.x) + RoomW / 2;
                    doorY = Oy(cb.y);
                }
                else // b below
                {
                    doorX = Ox(ca.x) + RoomW / 2;
                    doorY = Oy(ca.y);
                }

                grid[doorX, doorY] = CellRole.Floor;
            }

            return grid;
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

        // Fills a rectangle of the grid with a role, clamped to the grid bounds.
        private static void FillRect(CellRole[,] g, int x, int y, int w, int h, CellRole role)
        {
            for (var dx = 0; dx < w; dx++)
            for (var dy = 0; dy < h; dy++)
            {
                var px = x + dx;
                var py = y + dy;
                if (px >= 0 && py >= 0 && px < g.GetLength(0) && py < g.GetLength(1)) g[px, py] = role;
            }
        }
    }
}