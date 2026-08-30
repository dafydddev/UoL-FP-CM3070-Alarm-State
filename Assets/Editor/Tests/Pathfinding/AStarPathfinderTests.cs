using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests.Pathfinding
{
    // A* over the entry rules: routes are shortest, contiguous, and absent where no way exists.
    public class AStarPathfinderTests
    {
        // Rows read top-down, so the first row is the highest y; '#' is wall, anything else floor.
        private static readonly string[] Corridor =
        {
            "#######",
            "#.....#",
            "#######"
        };

        private static readonly string[] Maze =
        {
            "#########",
            "#...#...#",
            "#.#.#.#.#",
            "#.#...#.#",
            "#.#####.#",
            "#.......#",
            "#########"
        };

        private static readonly string[] SplitRooms =
        {
            "#####",
            "#.#.#",
            "#.#.#",
            "#####"
        };

        // The route includes the cell the mover stands on, so a walk of four steps is five cells.
        [Test]
        public void AStraightWalkIsTheStepsTakenPlusTheStartCell()
        {
            using var grid = new AsciiGrid(Corridor);

            var path = grid.Pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(5, 1), null);

            Assert.AreEqual(5, path.Count);
            Assert.AreEqual(new Vector2Int(1, 1), path[0]);
            Assert.AreEqual(new Vector2Int(5, 1), path[^1]);
        }

        // Every pair of floor cells, checked against a search that cannot take a detour.
        [Test]
        public void EveryPathIsAsShortAsABreadthFirstSearchMakesIt()
        {
            using var grid = new AsciiGrid(Maze);
            var floors = Floors(Maze);

            foreach (var start in floors)
            {
                var shortest = BreadthFirstDistances(Maze, start);
                foreach (var goal in floors)
                {
                    var path = grid.Pathfinder.FindPath(start, goal, null);
                    Assert.IsNotNull(path, $"{start} to {goal} should be reachable");
                    Assert.AreEqual(shortest[goal], path.Count - 1, $"{start} to {goal} took a detour");
                }
            }
        }

        [Test]
        public void APathIsAContiguousRunOfCardinalStepsWithNoCellTwice()
        {
            using var grid = new AsciiGrid(Maze);
            var start = new Vector2Int(1, 5);
            var goal = new Vector2Int(7, 5);

            var path = grid.Pathfinder.FindPath(start, goal, null);

            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[^1]);
            CollectionAssert.AllItemsAreUnique(path);
            for (var i = 1; i < path.Count; i++)
            {
                var step = path[i] - path[i - 1];
                Assert.AreEqual(1, Mathf.Abs(step.x) + Mathf.Abs(step.y), $"step {i} is not one cardinal move");
            }
        }

        [Test]
        public void AGoalWalledOffFromTheStartHasNoPath()
        {
            using var grid = new AsciiGrid(SplitRooms);

            Assert.IsNull(grid.Pathfinder.FindPath(new Vector2Int(1, 1), new Vector2Int(3, 1), null));
        }

        [Test]
        public void AnEndOfTheRouteThatCannotBeStoodOnHasNoPath()
        {
            using var grid = new AsciiGrid(Corridor);
            var floor = new Vector2Int(1, 1);

            Assert.IsNull(grid.Pathfinder.FindPath(new Vector2Int(0, 0), floor, null), "start is a wall");
            Assert.IsNull(grid.Pathfinder.FindPath(floor, new Vector2Int(0, 0), null), "goal is a wall");
            Assert.IsNull(grid.Pathfinder.FindPath(new Vector2Int(-1, 1), floor, null), "start is off the grid");
            Assert.IsNull(grid.Pathfinder.FindPath(floor, new Vector2Int(99, 99), null), "goal is off the grid");
        }

        // A null mover is the anonymous, keyless query the rules answer for.
        [Test]
        public void OnlyFloorInsideTheGridIsWalkable()
        {
            using var grid = new AsciiGrid(Corridor);

            Assert.IsTrue(grid.Pathfinder.IsWalkable(new Vector2Int(1, 1), null));
            Assert.IsFalse(grid.Pathfinder.IsWalkable(new Vector2Int(0, 1), null), "wall");
            Assert.IsFalse(grid.Pathfinder.IsWalkable(new Vector2Int(-1, 1), null));
            Assert.IsFalse(grid.Pathfinder.IsWalkable(new Vector2Int(7, 1), null));
        }

        [Test]
        public void ARouteBendsAroundABlockingOccupant()
        {
            using var grid = new AsciiGrid(
                "#####",
                "#...#",
                "#...#",
                "#####");

            var start = new Vector2Int(1, 1);
            var goal = new Vector2Int(3, 1);
            Assert.AreEqual(3, grid.Pathfinder.FindPath(start, goal, null).Count, "straight through, to begin with");

            grid.Block(new Vector2Int(2, 1));

            var diverted = grid.Pathfinder.FindPath(start, goal, null);

            Assert.AreEqual(5, diverted.Count, "the route should step around the barrier");
            CollectionAssert.DoesNotContain(diverted, new Vector2Int(2, 1));
        }

        private static List<Vector2Int> Floors(string[] rows)
        {
            var floors = new List<Vector2Int>();
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[0].Length; x++)
                {
                    if (rows[rows.Length - 1 - y][x] != '#') floors.Add(new Vector2Int(x, y));
                }
            }

            return floors;
        }

        // The independent shortest distance reference that the routes are measured against.
        private static Dictionary<Vector2Int, int> BreadthFirstDistances(string[] rows, Vector2Int start)
        {
            var distances = new Dictionary<Vector2Int, int> { [start] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in Dirs)
                {
                    var next = cell + dir;
                    if (next.x < 0 || next.y < 0 || next.x >= rows[0].Length || next.y >= rows.Length) continue;
                    if (rows[rows.Length - 1 - next.y][next.x] == '#') continue;
                    if (distances.ContainsKey(next)) continue;
                    distances[next] = distances[cell] + 1;
                    queue.Enqueue(next);
                }
            }

            return distances;
        }

        private static readonly Vector2Int[] Dirs =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };
    }
}