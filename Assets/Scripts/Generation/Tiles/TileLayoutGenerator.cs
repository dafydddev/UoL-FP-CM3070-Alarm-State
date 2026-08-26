using System.Collections.Generic;
using System.Linq;
using Generation.Cells;
using Graphs.Rooms;
using UnityEngine;

namespace Generation.Tiles
{
    // An axis-aligned room rectangle in tile coordinates, with derived bounds.
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

    // Turns a mission RoomGraph into a grid of structural roles:
    // - a placement strategy lays rooms out on an abstract cell grid,
    // - each room is marked as walls and floor and doorways are carved between connected rooms.
    public static class TileLayoutGenerator
    {
        private const int RoomW = 11; // room width in tiles
        private const int RoomH = 11; // room height in tiles

        // Convert an abstract cell coord to a tile origin; rooms overlap by 1 tile so walls are shared.
        private static int Ox(int cx) => cx * (RoomW - 1);
        private static int Oy(int cy) => cy * (RoomH - 1);

        // Relief is applied one room per round (at most 4 corridors each), so a congested level gains a handful of corridors.
        private const int WalkReliefRounds = 2;
        private const int MaxReliefRounds = 8;
        private const int StackedReliefRounds = 16; // extra rounds, run only on levels that stacked above

        // Builds the structural grid and outputs each room's tile rectangle.
        // The graph is mutated as relief corridors are added, and restored only if the level still stacks.
        public static CellRole[,] Generate(RoomGraph graph, TileLayoutStyle style, out Dictionary<string, RoomRect> roomRects)
        {
            Dictionary<string, List<string>> children;
            Dictionary<string, string> parent;
            string root;

            // The strategy decides only which abstract cell each room occupies and which pairs get a doorway:
            var cell = new Dictionary<string, Vector2Int>();
            var connections = new List<(string a, string b)>();

            BuildLookups();
            var relieved = new HashSet<string>(); // rooms already given relief corridors
            var useWalk = style == TileLayoutStyle.RandomWalk;
            for (var round = 0;; round++)
            {
                cell.Clear();
                connections.Clear();
                var clean = useWalk
                    ? RandomWalkLayout.Place(graph, root, children, cell, connections)
                    : SpineLayout.Place(graph, root, parent, children, cell, connections);
                if (clean) break;

                if (useWalk && round >= WalkReliefRounds)
                {
                    useWalk = false;
                    continue; // the spine may well embed this graph without further relief
                }

                // Too congested to embed: give the busiest room's branches a corridor to escape through, then retry.
                // The spine always completes, its greedy last resort may stack rooms, so the level still generates.
                if (round >= MaxReliefRounds || !SubdivideCongested(graph, relieved)) break;

                BuildLookups();
            }

            // Levels the loop above left with stacked rooms. Relieved sets can layer more corridors until the level embeds.
            // Retries build fresh layouts, so the accepted one is untouched unless a clean one lands.
            if (Stacked())
            {
                var rooms = new List<RoomNode>(graph.rooms);
                var edges = new List<RoomEdge>(graph.edges);
                var again = new HashSet<string>();
                for (var round = 0; Stacked() && round < StackedReliefRounds; round++)
                {
                    if (!SubdivideCongested(graph, again, $"_s{round}"))
                    {
                        if (again.Count == 0) break; // nothing congested at all
                        again.Clear(); // every congested room took a layer: start the next
                        continue;
                    }

                    BuildLookups();
                    var retryCell = new Dictionary<string, Vector2Int>();
                    var retryDoors = new List<(string a, string b)>();
                    if (SpineLayout.Place(graph, root, parent, children, retryCell, retryDoors))
                    {
                        cell = retryCell; // clean embedding: no room stacked
                        connections = retryDoors;
                    }
                }

                // Still stacked: the layered corridors bought nothing, so the graph goes back as it was.
                if (Stacked())
                {
                    graph.rooms = rooms;
                    graph.edges = edges;
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
            // Doors sit at the midpoint of the shared wall, which is what the wall placement passes rely on.
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

            // True while two rooms share a layout cell.
            bool Stacked() => cell.Values.Distinct().Count() < cell.Count;

            // Build parent/child lookups from the graph edges, tracking which rooms have a parent.
            // The root is the room with no parent (fall back to the first room).
            void BuildLookups()
            {
                children = new Dictionary<string, List<string>>();
                parent = new Dictionary<string, string>();
                var inbound = new HashSet<string>();
                foreach (var e in graph.edges)
                {
                    if (!children.TryGetValue(e.fromId, out var list)) children[e.fromId] = list = new List<string>();
                    list.Add(e.toId);
                    parent[e.toId] = e.fromId;
                    inbound.Add(e.toId);
                }

                root = graph.rooms.Find(r => !inbound.Contains(r.id))?.id ?? graph.rooms[0].id;
            }
        }

        // Emergency relief when a layout cannot be embedded, one room at a time.
        // The busiest not-yet-relieved room gets each outgoing edge subdivided with a pass-through corridor.
        private static bool SubdivideCongested(RoomGraph graph, HashSet<string> relieved, string idSuffix = "")
        {
            var degree = new Dictionary<string, int>();
            var outgoing = new Dictionary<string, int>();
            foreach (var r in graph.rooms)
            {
                degree[r.id] = 0;
                outgoing[r.id] = 0;
            }

            foreach (var e in graph.edges)
            {
                degree[e.fromId]++;
                degree[e.toId]++;
                outgoing[e.fromId]++;
            }

            // The busiest room: highest degree, then most branches; rooms-list order breaks ties, so the choice is deterministic.
            // A cell has four orthogonal neighbours, so congestion needs at least three doors; below that there is nothing to relieve.
            RoomNode target = null;
            foreach (var r in graph.rooms)
            {
                if (relieved.Contains(r.id) || degree[r.id] < 3 || outgoing[r.id] == 0) continue;
                if (target == null || degree[r.id] > degree[target.id] ||
                    (degree[r.id] == degree[target.id] && outgoing[r.id] > outgoing[target.id]))
                    target = r;
            }

            if (target == null) return false;

            relieved.Add(target.id);
            var relief = 0;
            foreach (var edge in new List<RoomEdge>(graph.edges))
            {
                if (edge.fromId != target.id) continue;
                // The lock moves to the far half, so the key still opens the way into the room beyond.
                var corridor = new RoomNode { id = $"room_relief_{target.id}_{relief++}{idSuffix}", type = RoomType.Corridor };
                graph.rooms.Add(corridor);
                var idx = graph.edges.IndexOf(edge);
                if (idx != -1) graph.edges.RemoveAt(idx);
                graph.edges.Add(new RoomEdge { fromId = edge.fromId, toId = corridor.id });
                graph.edges.Add(new RoomEdge { fromId = corridor.id, toId = edge.toId, locked = edge.locked, keyRoomId = edge.keyRoomId });
            }

            return true;
        }

        // Fills a rectangle of the grid with a role, clamped to the grid bounds.
        private static void FillRect(CellRole[,] g, int x, int y, int w, int h, CellRole role)
        {
            for (var dx = 0; dx < w; dx++)
            {
                for (var dy = 0; dy < h; dy++)
                {
                    var px = x + dx;
                    var py = y + dy;
                    if (px >= 0 && py >= 0 && px < g.GetLength(0) && py < g.GetLength(1)) g[px, py] = role;
                }
            }
        }
    }
}