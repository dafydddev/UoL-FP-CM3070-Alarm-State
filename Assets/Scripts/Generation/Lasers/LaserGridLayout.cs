using System;
using System.Collections.Generic;
using Generation.Tiles;
using UnityEngine;
using Random = System.Random;

namespace Generation.Lasers
{
    // One laser: the wall cell it is mounted on, the cardinal it fires along, and where in the cycle it lights.
    public readonly struct LaserSpec
    {
        public LaserSpec(Vector2Int emitter, Vector2Int direction, int phase)
        {
            Emitter = emitter;
            Direction = direction;
            Phase = phase;
        }

        public Vector2Int Emitter { get; }
        public Vector2Int Direction { get; }
        public int Phase { get; }
    }

    // Lays a pressure room's lasers out and says which are firing on a given tick.
    // Beams keep off the centre row and column and the axes alternate, so there is always a way across.
    public static class LaserGridLayout
    {
        // Lasers are placed in pairs, so an odd count lays out one short of it.
        public static List<LaserSpec> For(RoomRect rect, int lasers, int cyclePeriod, Random rng)
        {
            var half = Math.Max(1, cyclePeriod / 2);

            // Which axis leads varies per room, so neighbouring grids don't march in step.
            var verticalFirst = rng.Next(2) == 0;
            var verticals = lasers / 2;

            var specs = new List<LaserSpec>(lasers);
            foreach (var x in Lines(rect.CenterX, rect.X + 1, rect.Right - 2, verticals))
            {
                var fromTop = rng.Next(2) == 0;
                specs.Add(new LaserSpec(new Vector2Int(x, fromTop ? rect.Bottom - 1 : rect.Y),
                    fromTop ? Vector2Int.down : Vector2Int.up, verticalFirst ? 0 : half));
            }

            foreach (var y in Lines(rect.CenterY, rect.Y + 1, rect.Bottom - 2, lasers - verticals))
            {
                var fromLeft = rng.Next(2) == 0;
                specs.Add(new LaserSpec(new Vector2Int(fromLeft ? rect.X : rect.Right - 1, y),
                    fromLeft ? Vector2Int.right : Vector2Int.left, verticalFirst ? half : 0));
            }

            return specs;
        }

        // Evenly spaced pairs either side of the centre line, which is left clear.
        private static IEnumerable<int> Lines(int centre, int lo, int hi, int count)
        {
            var perSide = count / 2;
            var reach = Math.Min(centre - lo, hi - centre);

            for (var i = 1; i <= perSide; i++)
            {
                var step = Math.Max(i, (int)Math.Round(i * (reach + 1) / (double)(perSide + 1)));
                if (centre - step < lo || centre + step > hi) yield break;
                yield return centre - step;
                yield return centre + step;
            }
        }

        // Each laser is lit for half the cycle, the two phases half a cycle apart.
        public static bool IsLive(LaserSpec spec, int tick, int cyclePeriod)
        {
            var period = Math.Max(2, cyclePeriod);
            return (tick + spec.Phase) % period < period / 2;
        }

        // The cells a beam covers, stopping at the first block. Interior only, so it can't reach a doorway.
        public static List<Vector2Int> BeamCells(LaserSpec spec, RoomRect rect, Func<Vector2Int, bool> blocked)
        {
            var cells = new List<Vector2Int>();
            for (var cell = spec.Emitter + spec.Direction; Inside(rect, cell); cell += spec.Direction)
            {
                if (blocked != null && blocked(cell)) break;
                cells.Add(cell);
            }

            return cells;
        }

        private static bool Inside(RoomRect rect, Vector2Int cell) =>
            cell.x > rect.X && cell.x < rect.Right - 1 && cell.y > rect.Y && cell.y < rect.Bottom - 1;
    }
}
