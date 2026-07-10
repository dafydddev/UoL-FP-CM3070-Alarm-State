using System.Collections.Generic;
using Entities;
using Generation;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Scatters distraction pickups across the facility's walkable floor using Poisson disk sampling,
    // so no two land closer than minSeparation tiles apart (an even spread, never clumped).
    public class DistractionSpawner : EntitySpawner
    {
        [SerializeField] private GameObject distractionPrefab;
        [SerializeField] private int count = 8; // upper bound on distractions placed
        [SerializeField] private float minSeparation = 6f; // minimum spacing between distractions, in tiles
        private const int SampleAttempts = 30; // candidates tried per active point (Bridson's k)

        // Places up to count distractions on distinct, unoccupied floor cells with Poisson spacing.
        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Seed from the graph so distraction placement is repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Distraction, graph.level));

            var samples = PoissonSample(world.Grid.Width, world.Grid.Height, minSeparation, rng);

            var placed = 0;
            foreach (var sample in samples)
            {
                if (placed >= count) break;

                var cell = new Vector2Int(Mathf.FloorToInt(sample.x), Mathf.FloorToInt(sample.y));

                // Only walkable terrain, and never a cell something else already occupies.
                var tile = world.Grid.At(cell);
                if (!tile || tile.BlocksEntry(null)) continue;
                if (world.Occupancy.At(cell)) continue;

                var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(distractionPrefab, pos, Quaternion.identity, transform);

                var item = go.GetComponent<DistractionItem>();
                item.distractionId = $"distraction_{placed}";
                item.Init(world);
                go.name = $"Distraction_{placed}";
                placed++;
            }
        }

        // Bridson's Poisson disk sampling over [0,width] x [0,height]: returns points no closer than
        // radius apart, in roughly O(n). A background grid of cellSize = radius/sqrt(2) holds at most
        // one sample per cell, so proximity checks only touch a fixed neighbourhood.
        private static List<Vector2> PoissonSample(int width, int height, float radius, System.Random rng)
        {
            var samples = new List<Vector2>();
            if (radius <= 0f) return samples;

            var cellSize = radius / Mathf.Sqrt(2f);
            var cols = Mathf.CeilToInt(width / cellSize);
            var rows = Mathf.CeilToInt(height / cellSize);
            var grid = new int[cols, rows];
            for (var x = 0; x < cols; x++)
            for (var y = 0; y < rows; y++)
                grid[x, y] = -1; // -1 marks an empty background cell

            var active = new List<int>();

            // Seed the process with one random point.
            AddSample(new Vector2((float)rng.NextDouble() * width, (float)rng.NextDouble() * height),
                samples, active, grid, cellSize);

            while (active.Count > 0)
            {
                var activeIndex = rng.Next(active.Count);
                var origin = samples[active[activeIndex]];
                var found = false;

                for (var i = 0; i < SampleAttempts; i++)
                {
                    // A candidate in the annulus [radius, 2*radius] around the chosen point.
                    var angle = (float)rng.NextDouble() * 2f * Mathf.PI;
                    var dist = radius * (1f + (float)rng.NextDouble());
                    var candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

                    if (candidate.x < 0f || candidate.x >= width || candidate.y < 0f || candidate.y >= height) continue;
                    if (!FarEnough(candidate, samples, grid, cellSize, cols, rows, radius)) continue;

                    AddSample(candidate, samples, active, grid, cellSize);
                    found = true;
                    break;
                }

                // Exhausted its attempts: this point can spawn no more neighbours, so retire it.
                if (!found) active.RemoveAt(activeIndex);
            }

            return samples;
        }

        // Records a sample in the point list, the active list, and its background cell.
        private static void AddSample(Vector2 p, List<Vector2> samples, List<int> active, int[,] grid, float cellSize)
        {
            var index = samples.Count;
            samples.Add(p);
            active.Add(index);
            grid[(int)(p.x / cellSize), (int)(p.y / cellSize)] = index;
        }

        // True if no existing sample lies within radius of the candidate. Only the 5x5 block of
        // background cells around the candidate can hold a close enough point, so that is all we scan.
        private static bool FarEnough(Vector2 candidate, List<Vector2> samples, int[,] grid, float cellSize,
            int cols, int rows, float radius)
        {
            var cx = (int)(candidate.x / cellSize);
            var cy = (int)(candidate.y / cellSize);
            var radiusSqr = radius * radius;

            for (var x = Mathf.Max(0, cx - 2); x <= Mathf.Min(cols - 1, cx + 2); x++)
            for (var y = Mathf.Max(0, cy - 2); y <= Mathf.Min(rows - 1, cy + 2); y++)
            {
                var neighbour = grid[x, y];
                if (neighbour >= 0 && (samples[neighbour] - candidate).sqrMagnitude < radiusSqr) return false;
            }

            return true;
        }
    }
}
