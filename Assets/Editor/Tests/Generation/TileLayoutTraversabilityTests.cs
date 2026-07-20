using System.Collections.Generic;
using Generation.Cells;
using Generation.Tiles;
using Graphs.Rooms;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests.Generation
{
    // Grid-level traversability of the generated tile layout.
    // Every room centre must be reachable from the entrance by walking Floor cells through the carved doorways.
    public class TileLayoutTraversabilityTests
    {
        // A straight chain: entrance -> corridor -> primary -> exit.
        [Test]
        public void ChainLayoutIsFullyTraversable([Values] TileLayoutStyle style)
        {
            AssertAllRoomsReachable(ChainGraph(), style);
        }

        // A branch off the corridor, so the layout has to place rooms off the spine.
        [Test]
        public void BranchingLayoutIsFullyTraversable([Values] TileLayoutStyle style)
        {
            AssertAllRoomsReachable(BranchingGraph(), style);
        }

        // Generates the layout, floods Floor cells from the entrance, and asserts every room in the graph was reached.
        private static void AssertAllRoomsReachable(RoomGraph graph, TileLayoutStyle style)
        {
            var grid = TileLayoutGenerator.Generate(graph, style, out var rects);

            Assert.That(rects.Count, Is.EqualTo(graph.rooms.Count), "every room needs a rect");

            var entrance = rects["room_entrance"];
            var reached = FloodFill(grid, new Vector2Int(entrance.CenterX, entrance.CenterY));

            foreach (var room in graph.rooms)
            {
                var centre = new Vector2Int(rects[room.id].CenterX, rects[room.id].CenterY);
                Assert.IsTrue(reached.Contains(centre),
                    $"{room.id} ({room.type}) is unreachable from the entrance under {style}.");
            }
        }

        // 4-connected flood fill over Floor cells, returning every cell reached.
        private static HashSet<Vector2Int> FloodFill(CellRole[,] grid, Vector2Int start)
        {
            var width = grid.GetLength(0);
            var height = grid.GetLength(1);
            var seen = new HashSet<Vector2Int>();
            if (grid[start.x, start.y] != CellRole.Floor) return seen;

            var queue = new Queue<Vector2Int>();
            seen.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in Dirs)
                {
                    var next = cell + dir;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height) continue;
                    if (grid[next.x, next.y] != CellRole.Floor) continue;
                    if (seen.Add(next)) queue.Enqueue(next);
                }
            }

            return seen;
        }

        private static readonly Vector2Int[] Dirs =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        private static RoomGraph ChainGraph()
        {
            var graph = new RoomGraph { seed = 1, level = 1 };
            AddRoom(graph, "room_entrance", RoomType.Entrance);
            AddRoom(graph, "room_corridor_0", RoomType.Corridor);
            AddRoom(graph, "room_primary", RoomType.PrimaryObjectiveRoom);
            AddRoom(graph, "room_exit_0", RoomType.Exit);
            AddEdge(graph, "room_entrance", "room_corridor_0");
            AddEdge(graph, "room_corridor_0", "room_primary");
            AddEdge(graph, "room_primary", "room_exit_0");
            return graph;
        }

        private static RoomGraph BranchingGraph()
        {
            var graph = ChainGraph();
            AddRoom(graph, "room_key_0", RoomType.KeycardRoom);
            AddRoom(graph, "room_guard_0", RoomType.GuardPost);
            AddEdge(graph, "room_corridor_0", "room_key_0");
            AddEdge(graph, "room_corridor_0", "room_guard_0");
            return graph;
        }

        private static void AddRoom(RoomGraph graph, string id, RoomType type) =>
            graph.rooms.Add(new RoomNode { id = id, type = type });

        private static void AddEdge(RoomGraph graph, string from, string to) =>
            graph.edges.Add(new RoomEdge { fromId = from, toId = to });
    }
}