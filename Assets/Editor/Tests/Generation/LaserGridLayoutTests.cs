using System.Collections.Generic;
using System.Linq;
using Generation.Lasers;
using Generation.Tiles;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Editor.Tests.Generation
{
    // The laser grid a pressure room is fitted with: it stays crossable, and a seed lays the same grid out.
    public class LaserGridLayoutTests
    {
        private const int RoomSize = 11; // as TileLayoutGenerator builds them
        private const int Lasers = 4;
        private const int Period = 16;
        private const int Seeds = 50;

        private static RoomRect Room() => new(0, 0, RoomSize, RoomSize);

        // Somewhere to wait out a half of the cycle, in every room the layout can produce.
        [Test]
        public void TheCentreCellIsNeverLive()
        {
            var rect = Room();
            var centre = new Vector2Int(rect.CenterX, rect.CenterY);

            ForEverySeedAndTick(rect, (live, seed, tick) =>
                Assert.That(live.Contains(centre), Is.False, $"seed {seed} lights the centre on tick {tick}"));
        }

        // A crossing is a leg along the centre column and one along the centre row; one is always clear.
        [Test]
        public void TheCentreRowOrColumnIsClearAtEveryTick()
        {
            var rect = Room();

            ForEverySeedAndTick(rect, (live, seed, tick) =>
            {
                var columnClear = Line(rect, vertical: true).All(cell => !live.Contains(cell));
                var rowClear = Line(rect, vertical: false).All(cell => !live.Contains(cell));
                Assert.That(columnClear || rowClear, Is.True,
                    $"seed {seed} seals both the centre row and the centre column on tick {tick}");
            });
        }

        // A beam that reached the wall ring would cover a doorway or spill into the next room.
        [Test]
        public void BeamsStayOffTheWalls()
        {
            var rect = Room();
            for (var seed = 0; seed < Seeds; seed++)
                foreach (var spec in Layout(seed))
                foreach (var cell in LaserGridLayout.BeamCells(spec, rect, null))
                {
                    Assert.That(cell.x, Is.InRange(rect.X + 1, rect.Right - 2), $"seed {seed} runs a beam into a wall");
                    Assert.That(cell.y, Is.InRange(rect.Y + 1, rect.Bottom - 2),
                        $"seed {seed} runs a beam into a wall");
                }
        }

        // An emitter on a wall midpoint would sit in a doorway.
        [Test]
        public void EmittersAvoidTheDoorways()
        {
            var rect = Room();
            var doorways = new[]
            {
                new Vector2Int(rect.CenterX, rect.Y), new Vector2Int(rect.CenterX, rect.Bottom - 1),
                new Vector2Int(rect.X, rect.CenterY), new Vector2Int(rect.Right - 1, rect.CenterY),
            };

            for (var seed = 0; seed < Seeds; seed++)
                foreach (var spec in Layout(seed))
                    Assert.That(doorways.Contains(spec.Emitter), Is.False,
                        $"seed {seed} mounts a laser in a doorway");
        }

        // Lasers of an axis are spaced evenly either side of the centre, not bunched to one side of the room.
        [Test]
        public void LasersAreSpacedEvenlyAboutTheCentre()
        {
            var rect = Room();
            for (var seed = 0; seed < Seeds; seed++)
            {
                var specs = Layout(seed);
                var columns = specs.Where(s => s.Direction.x == 0).Select(s => s.Emitter.x).OrderBy(x => x).ToList();
                var rows = specs.Where(s => s.Direction.y == 0).Select(s => s.Emitter.y).OrderBy(y => y).ToList();

                Assert.That(rect.CenterX - columns[0], Is.EqualTo(columns[^1] - rect.CenterX),
                    $"seed {seed} sits its columns off centre");
                Assert.That(rect.CenterY - rows[0], Is.EqualTo(rows[^1] - rect.CenterY),
                    $"seed {seed} sits its rows off centre");
                Assert.That(columns[0] - rect.X, Is.GreaterThan(1), $"seed {seed} runs a column against the wall");
                Assert.That(rows[0] - rect.Y, Is.GreaterThan(1), $"seed {seed} runs a row against the wall");
            }
        }

        // Half the lasers fire at a time, so the room is never all-clear and never sealed.
        [Test]
        public void ExactlyOneAxisFiresAtATime()
        {
            for (var seed = 0; seed < Seeds; seed++)
            {
                var specs = Layout(seed);
                for (var tick = 0; tick < Period * 2; tick++)
                {
                    var firing = specs.Count(spec => LaserGridLayout.IsLive(spec, tick, Period));
                    Assert.That(firing, Is.EqualTo(specs.Count / 2),
                        $"seed {seed} fires {firing} lasers on tick {tick}");
                }
            }
        }

        [Test]
        public void TheSameSeedLaysTheSameGrid()
        {
            var first = Layout(7);
            var second = Layout(7);

            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Emitter, Is.EqualTo(first[i].Emitter));
                Assert.That(second[i].Direction, Is.EqualTo(first[i].Direction));
                Assert.That(second[i].Phase, Is.EqualTo(first[i].Phase));
            }
        }

        [Test]
        public void ASmallerRoomStillLaysOut()
        {
            var specs = LaserGridLayout.For(new RoomRect(0, 0, 5, 5), Lasers, Period, new Random(1));

            Assert.That(specs, Is.Not.Empty);
            Assert.That(specs.Count, Is.LessThanOrEqualTo(Lasers));
        }

        private static List<LaserSpec> Layout(int seed) =>
            LaserGridLayout.For(Room(), Lasers, Period, new Random(seed));

        // Every cell a live beam covers on this tick.
        private static HashSet<Vector2Int> LiveCells(RoomRect rect, IEnumerable<LaserSpec> specs, int tick)
        {
            var live = new HashSet<Vector2Int>();
            foreach (var spec in specs.Where(s => LaserGridLayout.IsLive(s, tick, Period)))
                live.UnionWith(LaserGridLayout.BeamCells(spec, rect, null));

            return live;
        }

        // The room's centre column or centre row, doorway to doorway.
        private static IEnumerable<Vector2Int> Line(RoomRect rect, bool vertical)
        {
            for (var i = 0; i < RoomSize; i++)
                yield return vertical
                    ? new Vector2Int(rect.CenterX, rect.Y + i)
                    : new Vector2Int(rect.X + i, rect.CenterY);
        }

        private static void ForEverySeedAndTick(RoomRect rect, System.Action<HashSet<Vector2Int>, int, int> assert)
        {
            for (var seed = 0; seed < Seeds; seed++)
            {
                var specs = Layout(seed);
                for (var tick = 0; tick < Period * 2; tick++) assert(LiveCells(rect, specs, tick), seed, tick);
            }
        }
    }
}