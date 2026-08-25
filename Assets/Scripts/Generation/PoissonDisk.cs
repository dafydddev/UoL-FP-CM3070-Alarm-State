using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    // Bridson's Poisson disk sampling over the facility tiles.
    // Yields cells no two of which are closer than the radius: no clumps, no bare patches.
    public static class PoissonDisk
    {
        private const int Candidates = 30; // tries per active sample before it is retired

        // Blue-noise cells in the inclusive box [x, xMax] x [y, yMax], at least radius apart.
        public static List<Vector2Int> Sample(int x, int y, int xMax, int yMax, float radius, Random rng)
        {
            var w = xMax - x + 1;
            var h = yMax - y + 1;
            var samples = new List<Vector2Int>();
            if (w <= 0 || h <= 0 || radius <= 0f) return samples;

            // Background grid sized so each bucket holds at most one sample.
            var bucket = radius / Mathf.Sqrt(2f);
            var gw = Mathf.CeilToInt(w / bucket);
            var gh = Mathf.CeilToInt(h / bucket);
            var grid = new int[gw][];
            for (var index = 0; index < gw; index++)
            {
                grid[index] = new int[gh];
            }

            for (var i = 0; i < gw; i++)
            {
                for (var j = 0; j < gh; j++)
                {
                    grid[i][j] = -1;
                }
            }

            var points = new List<Vector2>();
            var active = new List<int>();

            // Seed the walk from a random cell centre inside the box.
            Add(new Vector2(rng.Next(w) + 0.5f, rng.Next(h) + 0.5f));

            while (active.Count > 0)
            {
                var pick = rng.Next(active.Count);
                var from = points[active[pick]];
                var placed = false;

                for (var k = 0; k < Candidates && !placed; k++)
                {
                    // Candidate in the annulus [radius, 2 * radius] around the active sample.
                    var angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                    var distance = radius * (1f + (float)rng.NextDouble());
                    var candidate = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (candidate.x < 0f || candidate.y < 0f || candidate.x >= w || candidate.y >= h) continue;
                    if (!IsFarEnough(candidate)) continue;
                    Add(candidate);
                    placed = true;
                }

                if (!placed) active.RemoveAt(pick); // exhausted: this sample takes no more neighbours
            }

            return samples;

            void Add(Vector2 p)
            {
                grid[(int)(p.x / bucket)][(int)(p.y / bucket)] = points.Count;
                active.Add(points.Count);
                points.Add(p);
                samples.Add(new Vector2Int(x + (int)p.x, y + (int)p.y));
            }

            // Only the 5x5 block of buckets around a candidate can hold a sample within the radius.
            bool IsFarEnough(Vector2 p)
            {
                var gx = (int)(p.x / bucket);
                var gy = (int)(p.y / bucket);
                for (var i = Mathf.Max(gx - 2, 0); i <= Mathf.Min(gx + 2, gw - 1); i++)
                {
                    for (var j = Mathf.Max(gy - 2, 0); j <= Mathf.Min(gy + 2, gh - 1); j++)
                    {
                        var index = grid[i][j];
                        if (index >= 0 && (points[index] - p).sqrMagnitude < radius * radius) return false;
                    }
                }

                return true;
            }
        }
    }
}