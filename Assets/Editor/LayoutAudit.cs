using System;
using System.Collections.Generic;
using System.Linq;
using Generation.Tiles;
using Graphs.Missions;
using Graphs.Rooms;
using Run;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // In-editor audit of the tile-layout strategies.
    // Runs the real generation pipeline over many seeds, levels and difficulty profiles, then reports violations:
    // - rooms with more than one parent, or over the four-door budget, or unreachable
    // - two rooms stacked on the same layout cell
    // - doored room pairs that are not orthogonally adjacent
    // - rooms missing from the layout output
    // - relief corridors the layout had to add to make a congested graph fit the grid
    // LayoutAuditWindow (Tools -> Layout Audit) is the front-end.

    public static class LayoutAudit
    {
        private const string DifficultyDir = "Assets/Scriptable Objects/Difficulties";
        private static readonly string[] ShippedProfiles = { "Easy", "Normal", "Hard" };
        public const int TotalLevels = 20;
        public const int QuickSeeds = 150;
        public const int ThoroughSeeds = 500;

        // Accumulated counts for one profile-and-style (or the shared graph checks).
        public sealed class Stats
        {
            public long Levels, MultiParent, OverBudget, Unreachable;
            public long StackedLevels, StackedCells, NonAdjLevels, NonAdjEdges, Missing;
            public long ReliefLevels, ReliefRooms, TotalRooms;
            public int MaxRooms;

            // Relief corridors are reported but not counted against the run: they are the fix, not the fault.
            public bool HasViolation => MultiParent + OverBudget + Unreachable + StackedLevels + NonAdjLevels + Missing > 0;
        }

        // One difficulty profile's graph checks plus both layout styles.
        public sealed class ProfileResult
        {
            public string Name;
            public readonly Stats Graph = new();
            public readonly Stats Spine = new();
            public readonly Stats Walk = new();

            public bool HasViolation => Graph.HasViolation || Spine.HasViolation || Walk.HasViolation;
            public double AvgRooms => Graph.Levels > 0 ? (double)Graph.TotalRooms / Graph.Levels : 0;
        }

        // A complete audit run, as consumed by LayoutAuditWindow.
        public sealed class AuditResult
        {
            public int Seeds;
            public double DurationSeconds;
            public DateTime CompletedAt;
            public readonly List<ProfileResult> Profiles = new();

            public bool AnyViolation => Profiles.Exists(p => p.HasViolation);
        }

        // Runs the full audit. Returns null if cancelled or no difficulty profiles could be loaded.
        public static AuditResult RunAudit(int seeds, bool includeStress)
        {
            var profiles = LoadProfiles(includeStress);
            if (profiles.Count == 0)
            {
                Debug.LogError($"Layout Audit: no difficulty profiles found under \"{DifficultyDir}\".");
                return null;
            }

            // MissionGenerator is a MonoBehaviour, so it needs to live on a GameObject.
            var host = new GameObject("~LayoutAuditMissionGenerator") { hideFlags = HideFlags.HideAndDontSave };
            var mission = host.AddComponent<MissionGenerator>();
            mission.randomSeed = false; // drive the seed ourselves for reproducibility
            mission.randomType = true;

            var result = new AuditResult { Seeds = seeds };
            var timer = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                for (var p = 0; p < profiles.Count; p++)
                {
                    var (name, profile) = profiles[p];
                    var profileResult = new ProfileResult { Name = name };

                    for (var seed = 0; seed < seeds; seed++)
                    {
                        // Polled every sixteenth seed, as the progress bar costs more than a level does.
                        if ((seed & 15) == 0 &&
                            EditorUtility.DisplayCancelableProgressBar("Layout Audit",
                                $"{name}: seed {seed}/{seeds}", (p + seed / (float)seeds) / profiles.Count))
                        {
                            Debug.LogWarning("Layout Audit: cancelled.");
                            return null;
                        }

                        mission.seed = seed;
                        for (var level = 1; level <= TotalLevels; level++)
                        {
                            var m = mission.Generate(profile, level, TotalLevels);

                            // Fresh room graph per style: the layout mutates the graph (relief corridors).
                            var forSpine = RoomGraphGenerator.Generate(m, profile, level, TotalLevels);
                            CheckGraph(forSpine, profileResult.Graph);
                            CheckLayout(forSpine, TileLayoutStyle.Spine, profileResult.Spine);
                            var forWalk = RoomGraphGenerator.Generate(m, profile, level, TotalLevels);
                            CheckLayout(forWalk, TileLayoutStyle.RandomWalk, profileResult.Walk);
                        }
                    }

                    result.Profiles.Add(profileResult);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                foreach (var (_, profile) in profiles)
                {
                    if (profile && !AssetDatabase.Contains(profile))
                    {
                        UnityEngine.Object.DestroyImmediate(profile); // only the synthetic stress profile
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            result.DurationSeconds = timer.Elapsed.TotalSeconds;
            result.CompletedAt = DateTime.Now;
            return result;
        }

        // The graph as the generator left it, before any layout has had a chance to add relief corridors.
        private static void CheckGraph(RoomGraph g, Stats s)
        {
            s.Levels++;
            s.TotalRooms += g.rooms.Count;
            s.MaxRooms = Mathf.Max(s.MaxRooms, g.rooms.Count);

            var inbound = new Dictionary<string, int>();
            var degree = new Dictionary<string, int>();
            var children = new Dictionary<string, List<string>>();
            foreach (var r in g.rooms)
            {
                inbound[r.id] = 0;
                degree[r.id] = 0;
            }

            foreach (var e in g.edges)
            {
                inbound[e.toId]++;
                degree[e.fromId]++;
                degree[e.toId]++;
                if (!children.TryGetValue(e.fromId, out var kids)) children[e.fromId] = kids = new List<string>();
                kids.Add(e.toId);
            }

            s.MultiParent += inbound.Count(kv => kv.Value > 1);
            s.OverBudget += degree.Count(kv => kv.Value > 4);

            var root = g.rooms.Find(r => inbound[r.id] == 0)?.id ?? g.rooms[0].id;
            var seen = new HashSet<string> { root };
            var queue = new Queue<string>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                if (!children.TryGetValue(queue.Dequeue(), out var kids)) continue;
                foreach (var k in kids.Where(seen.Add))
                {
                    queue.Enqueue(k);
                }
            }

            s.Unreachable += g.rooms.Count - seen.Count;
        }

        private static void CheckLayout(RoomGraph g, TileLayoutStyle style, Stats s)
        {
            s.Levels++;
            // Counted before and after, as the only sign of relief is rooms the graph did not have going in.
            var roomsBefore = g.rooms.Count;
            TileLayoutGenerator.Generate(g, style, out var rects);
            if (g.rooms.Count > roomsBefore)
            {
                s.ReliefLevels++;
                s.ReliefRooms += g.rooms.Count - roomsBefore;
            }

            // Rect origins are cell * (RoomW - 1). Infer the stride rather than hardcoding room size.
            // The smallest non-zero origin is one cell across, as the layout is normalised to start at (0, 0).
            var stride = int.MaxValue;
            foreach (var r in rects.Values)
            {
                if (r.X > 0) stride = Mathf.Min(stride, r.X);
                if (r.Y > 0) stride = Mathf.Min(stride, r.Y);
            }

            if (stride == int.MaxValue) stride = 1; // single-room layout
            var cells = rects.ToDictionary(kv => kv.Key,
                kv => new Vector2Int(kv.Value.X / stride, kv.Value.Y / stride));

            s.Missing += g.rooms.Count(r => !cells.ContainsKey(r.id));

            var stacked = cells.Values.GroupBy(c => c).Count(grp => grp.Count() > 1);
            if (stacked > 0)
            {
                s.StackedLevels++;
                s.StackedCells += stacked;
            }

            // A door between rooms that are not neighbours would be carved into a wall with nothing behind it.
            var nonAdj = 0;
            foreach (var e in g.edges)
            {
                if (!cells.TryGetValue(e.fromId, out var a) || !cells.TryGetValue(e.toId, out var b)) continue;
                if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1) nonAdj++;
            }

            if (nonAdj <= 0) return;
            s.NonAdjLevels++;
            s.NonAdjEdges += nonAdj;
        }

        private static List<(string name, RunDifficulty profile)> LoadProfiles(bool includeStress)
        {
            var list = new List<(string, RunDifficulty)>();
            foreach (var n in ShippedProfiles)
            {
                var asset = AssetDatabase.LoadAssetAtPath<RunDifficulty>($"{DifficultyDir}/{n}.asset");
                if (asset) list.Add((n, asset));
                else Debug.LogWarning($"Layout Audit: profile \"{n}\" not found under \"{DifficultyDir}\".");
            }

            if (includeStress) list.Add(("Stress", MakeStress()));
            return list;
        }

        // A temporary, synthetic worst-case profile sweeping past the shipped assets.
        private static RunDifficulty MakeStress()
        {
            var p = ScriptableObject.CreateInstance<RunDifficulty>();
            p.label = "Stress";
            p.secondaryObjectives = Flat(0, 6);
            p.extraExits = Flat(0, 6);
            p.lockChance = Flat(0, 1);
            p.guardChance = Flat(0, 1);
            return p;
        }

        // A range that reads the same at every level, so a test's figures don't drift with difficulty progress.
        private static RunDifficulty.Range Flat(float min, float max) => new()
        {
            minFloor = min, minCeiling = min, maxFloor = max, maxCeiling = max,
            minShape = AnimationCurve.Linear(0, 0, 1, 1), maxShape = AnimationCurve.Linear(0, 0, 1, 1),
        };
    }
}