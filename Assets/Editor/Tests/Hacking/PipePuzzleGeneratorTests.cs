using System.Collections.Generic;
using Hacking;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests.Hacking
{
    // Solvability of the generated hacking board.
    // An unsolvable board would strand the player on an objective with no way to finish it.
    public class PipePuzzleGeneratorTests
    {
        private const int Seeds = 25;

        [Test]
        public void EveryGeneratedBoardCanBeSolvedByRotatingTiles([Values(3, 4, 5, 6)] int size)
        {
            for (var seed = 0; seed < Seeds; seed++)
            {
                var board = PipePuzzleGenerator.Generate(new System.Random(seed), size,
                    complexity: 0.5f, decoyPaths: 2, scrambleChance: 1f);

                Assert.IsTrue(IsSolvable(board), $"size {size} seed {seed} generated an unsolvable board");
            }
        }

        // The scramble pass is meant to guarantee the player always has something to untangle.
        [Test]
        public void NoGeneratedBoardArrivesAlreadySolved([Values(3, 4, 5, 6)] int size)
        {
            for (var seed = 0; seed < Seeds; seed++)
            {
                var board = PipePuzzleGenerator.Generate(new System.Random(seed), size,
                    complexity: 0.5f, decoyPaths: 2, scrambleChance: 1f);

                Assert.IsFalse(board.TryTraceCircuit(out _), $"size {size} seed {seed} arrived already solved");
            }
        }

        [Test]
        public void BoardsSmallerThanTheMinimumAreClampedUp()
        {
            var board = PipePuzzleGenerator.Generate(new System.Random(1), size: 1,
                complexity: 0.5f, decoyPaths: 0, scrambleChance: 0f);

            Assert.That(board.Width, Is.GreaterThanOrEqualTo(3));
            Assert.That(board.Height, Is.EqualTo(board.Width));
        }

        [Test]
        public void TheSameSeedProducesTheSameBoard()
        {
            var first = PipePuzzleGenerator.Generate(new System.Random(7), 5, 0.5f, 2, 1f);
            var second = PipePuzzleGenerator.Generate(new System.Random(7), 5, 0.5f, 2, 1f);

            Assert.That(second.StartCell, Is.EqualTo(first.StartCell));
            Assert.That(second.EndCell, Is.EqualTo(first.EndCell));
            Assert.That(Describe(second), Is.EqualTo(Describe(first)));
        }

        private static string Describe(PipeBoard board)
        {
            var description = new System.Text.StringBuilder();
            for (var x = 0; x < board.Width; x++)
            for (var y = 0; y < board.Height; y++)
            {
                var tile = board.At(new Vector2Int(x, y));
                description.Append($"{tile.Type}:{tile.Rotation}|");
            }

            return description.ToString();
        }

        private static readonly PipeDirection[] Sides =
        {
            PipeDirection.North, PipeDirection.East, PipeDirection.South, PipeDirection.West
        };

        // A route exists when every cell along it can be turned to carry the flow through.
        // Rotations are chosen per cell, which is exactly what the player does.
        private static bool IsSolvable(PipeBoard board) =>
            Walk(board, board.StartCell, PipeDirection.West, new HashSet<Vector2Int>());

        private static bool Walk(PipeBoard board, Vector2Int cell, PipeDirection entry,
            HashSet<Vector2Int> onPath)
        {
            if (board.At(cell) == null || !onPath.Add(cell)) return false;

            var tile = board.At(cell);
            if (cell == board.EndCell && CanOpen(tile, entry | PipeDirection.East)) return true;

            foreach (var exit in Sides)
            {
                if (exit == entry || !CanOpen(tile, entry | exit)) continue;
                if (Walk(board, cell + exit.Offset(), exit.Opposite(), onPath)) return true;
            }

            onPath.Remove(cell); // free the cell for other routes
            return false;
        }

        // Whether some rotation of the tile opens all the given sides at once.
        private static bool CanOpen(PipeTile tile, PipeDirection sides)
        {
            for (var rotation = 0; rotation < 4; rotation++)
                if ((tile.Type.Ends().Rotated(rotation) & sides) == sides)
                    return true;

            return false;
        }
    }
}
