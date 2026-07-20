using System.Collections.Generic;
using System.Linq;
using Generation;
using UnityEngine;

namespace Hacking
{
    // Builds a hacking board from a seeded RNG so every puzzle is reproducible.
    public static class PipePuzzleGenerator
    {
        private const int MinSize = 3; // below this a board can't hold a non-trivial circuit
        private const int MaxDecoyLength = 3;
        private const int MaxAttempts = 8; // bounded retries for the passes that hunt for a free cell

        // The carve's step budget is a worst-case time cap, not a tuning target;
        // the direct fallback below keeps generation infallible when it runs out.
        private const int StepsPerCell = 40;

        // The four sides in clockwise order, for the passes that pick or shuffle directions.
        private static readonly PipeDirection[] Sides =
        {
            PipeDirection.North, PipeDirection.East, PipeDirection.South, PipeDirection.West
        };

        public static PipeBoard Generate(System.Random rng, int size, float complexity, int decoyPaths,
            float scrambleChance)
        {
            size = Mathf.Max(size, MinSize);
            var start = new Vector2Int(0, rng.Next(size));
            var end = new Vector2Int(size - 1, rng.Next(size));

            // 1. Carve the solution, then record the sides each of its cells must open:
            // the feed into the start tile, each step to its neighbour, and the outlet east.
            var solution = CarveSolution(rng, size, start, end, complexity);
            var ends = new PipeDirection[size, size];
            ends[start.x, start.y] |= PipeDirection.West;
            ends[end.x, end.y] |= PipeDirection.East;
            for (var i = 1; i < solution.Count; i++)
            {
                var side = SideBetween(solution[i - 1], solution[i]);
                ends[solution[i - 1].x, solution[i - 1].y] |= side;
                ends[solution[i].x, solution[i].y] |= side.Opposite();
            }

            // 2. Hang misleading dead-end branches off the solution.
            AddDecoys(rng, ends, solution, size, decoyPaths);

            // 3. Realise tiles: carved cells get the shape their sides demand,
            // in the solved rotation for now; untouched cells get random filler.
            var tiles = new PipeTile[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var y = 0; y < size; y++)
                {
                    var cell = new Vector2Int(x, y);
                    tiles[x, y] = ends[x, y] == PipeDirection.None
                        ? RandomTile(rng, cell)
                        : TileFor(cell, ends[x, y]);
                }
            }

            // 4. Twist the starting rotations so the player has a knot to untangle.
            var board = new PipeBoard(tiles, start, end);
            Scramble(rng, board, solution, scrambleChance);
            return board;
        }

        // Self-avoiding walk from start to end, backtracking out of dead ends so it always arrives.
        // Complexity is the chance each step wanders instead of heading for the end node, so harder boards carry longer, twistier circuits.
        private static List<Vector2Int> CarveSolution(System.Random rng, int size, Vector2Int start, Vector2Int end,
            float complexity)
        {
            var steps = StepsPerCell * size * size;
            var path = new List<Vector2Int> { start };
            var visited = new HashSet<Vector2Int> { start };

            return Step(start) ? path : DirectSolution(start, end);

            bool Step(Vector2Int cell)
            {
                if (cell == end) return true;
                foreach (var side in Candidates(rng, cell, end, complexity))
                {
                    if (steps-- <= 0) return false;
                    var next = cell + side.Offset();
                    if (next.x < 0 || next.y < 0 || next.x >= size || next.y >= size) continue;
                    if (!visited.Add(next)) continue;

                    path.Add(next);
                    if (Step(next)) return true;
                    path.RemoveAt(path.Count - 1);
                    visited.Remove(next);
                }

                return false;
            }
        }

        // Candidate sides for the next carve step,
        // shuffled then stable-sorted so sides that close the distance to the end come first.
        private static PipeDirection[] Candidates(System.Random rng, Vector2Int cell, Vector2Int end, float complexity)
        {
            var sides = Shuffle.Copy(Sides, rng);
            if (rng.NextDouble() < complexity) return sides; // wander: keep the shuffle
            return sides.OrderBy(s => Distance(cell + s.Offset(), end)).ToArray();
        }

        // Manhattan distance, the step count between cells on the 4-connected board.
        private static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // The no-search fallback circuit: east along the start row, then along the end column.
        private static List<Vector2Int> DirectSolution(Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int> { start };
            var cell = start;
            while (cell.x < end.x)
            {
                cell += Vector2Int.right;
                path.Add(cell);
            }

            while (cell.y != end.y)
            {
                cell += cell.y < end.y ? Vector2Int.up : Vector2Int.down;
                path.Add(cell);
            }

            return path;
        }

        // The side of cell a that faces the orthogonally adjacent cell b.
        private static PipeDirection SideBetween(Vector2Int a, Vector2Int b)
        {
            if (b.x > a.x) return PipeDirection.East;
            if (b.x < a.x) return PipeDirection.West;
            return b.y > a.y ? PipeDirection.North : PipeDirection.South;
        }

        // Dead-end branches hung off random solution cells.
        // Each one opens an extra side on its host then wanders a few cells before stopping,
        // so it reads as a live route until the player chases it.
        private static void AddDecoys(System.Random rng, PipeDirection[,] ends, List<Vector2Int> solution, int size,
            int count)
        {
            for (var i = 0; i < count; i++)
            {
                // Hunt for a solution cell with an untouched neighbour to branch into.
                for (var attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    // Never the start cell: it keeps its two sides so a rotation can always shut the feed.
                    var host = solution[rng.Next(1, solution.Count)];
                    var side = Sides[rng.Next(Sides.Length)];
                    var cell = host + side.Offset();
                    if (!Untouched(cell)) continue;

                    ends[host.x, host.y] |= side;

                    // Wander the branch onward through untouched cells until it dead-ends.
                    var entry = side.Opposite();
                    var length = rng.Next(1, MaxDecoyLength + 1);
                    for (var step = 0; step < length; step++)
                    {
                        ends[cell.x, cell.y] |= entry;
                        var onward = Sides[rng.Next(Sides.Length)];
                        var next = cell + onward.Offset();
                        if (onward == entry || !Untouched(next)) break;
                        ends[cell.x, cell.y] |= onward;
                        cell = next;
                        entry = onward.Opposite();
                    }

                    break;
                }
            }

            return;

            bool Untouched(Vector2Int cell) =>
                cell is { x: >= 0, y: >= 0 } && cell.x < size && cell.y < size &&
                ends[cell.x, cell.y] == PipeDirection.None;
        }

        // The shape and solved rotation for a carved cell's required sides. A decoy tail
        // the carving reached from only one side becomes a capped stub.
        private static PipeTile TileFor(Vector2Int cell, PipeDirection sides)
        {
            var type = CountSides(sides) switch
            {
                1 => PipeType.Cap,
                2 => sides.Rotated(2) == sides
                    ? PipeType.Straight
                    : PipeType.Elbow, // only opposite ends survive a half turn
                3 => PipeType.Tee,
                _ => PipeType.Cross,
            };

            // Find the rotation that lines the shape's ends up with the required sides.
            for (var rotation = 0; rotation < 4; rotation++)
                if (type.Ends().Rotated(rotation) == sides)
                    return new PipeTile { Cell = cell, Type = type, Rotation = rotation };

            return new PipeTile { Cell = cell, Type = type }; // unreachable: every 2-4 side mask has a rotation
        }

        // How many of a mask's four sides are open.
        private static int CountSides(PipeDirection sides)
        {
            var bits = (int)sides;
            var count = 0;
            while (bits != 0)
            {
                count += bits & 1;
                bits >>= 1;
            }

            return count;
        }

        // Filler for cells the carving never touched.
        // Straights and elbows dominate so the board doesn't read as connect-anything,
        // with the odd tee and capped stub for noise; no crosses, which join whatever they meet.
        private static PipeTile RandomTile(System.Random rng, Vector2Int cell) => new()
        {
            Cell = cell,
            Type = rng.Next(8) switch
            {
                0 or 1 => PipeType.Straight,
                2 or 3 or 4 => PipeType.Elbow,
                5 => PipeType.Tee,
                _ => PipeType.Cap,
            },
            Rotation = rng.Next(4),
        };

        // Randomises starting rotations — scrambleChance is the odds each tile is disturbed —
        // then makes sure the board can't open already solved.
        private static void Scramble(System.Random rng, PipeBoard board, List<Vector2Int> solution,
            float scrambleChance)
        {
            for (var x = 0; x < board.Width; x++)
            for (var y = 0; y < board.Height; y++)
            {
                if (rng.NextDouble() >= scrambleChance) continue;
                board.At(new Vector2Int(x, y)).Rotation = rng.Next(4);
            }

            // A board that arrives solved isn't a puzzle: twist solution tiles until it holds a fault.
            for (var attempt = 0; attempt < MaxAttempts && board.TryTraceCircuit(out _); attempt++)
            {
                var tile = board.At(solution[rng.Next(solution.Count)]);
                tile.Rotation = (tile.Rotation + rng.Next(1, 4)) & 3;
            }

            // Should the twists run out, shut the feed instead.
            // Power cannot enter a start tile where the west face is closed, so the board cannot open solved.
            if (board.TryTraceCircuit(out _)) CloseFeed(board);
        }

        // Turns the start tile until the side the feed enters by is shut.
        private static void CloseFeed(PipeBoard board)
        {
            var tile = board.At(board.StartCell);
            for (var turn = 0; turn < 4 && (tile.Connections & PipeDirection.West) != 0; turn++)
            {
                tile.Rotate();
            }
        }
    }
}