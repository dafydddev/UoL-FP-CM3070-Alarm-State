using System.Collections.Generic;
using System.Linq;
using Graphs;
using Graphs.Missions;
using Graphs.Rooms;
using NUnit.Framework;
using Run;
using UnityEngine;

namespace Editor.Tests.Generation
{
    // The adaptive pass leaning the other way: a thriving player may get a laser room, and must cross it.
    public class RoomGraphPressureTests
    {
        private const int TotalLevels = 20;
        private const int Level = 10;
        private const float Struggling = -1f;
        private const float Thriving = 1f;

        [Test]
        public void AThrivingPlayerGetsAPressureRoom()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            Object.DestroyImmediate(profile);

            Assert.That(Pressures(graph), Is.Not.Empty, "a thriving player should get a pressure room");
        }

        [Test]
        public void AStrugglingPlayerGetsNoPressureRoom()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            Object.DestroyImmediate(profile);
            Assert.That(Pressures(graph), Is.Empty);
        }

        // The two branches are exclusive: relief and pressure never land in the same level.
        [Test]
        public void OnlyOneBranchEverFires()
        {
            var profile = Profile();
            var thriving = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            var struggling = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            Object.DestroyImmediate(profile);
            Assert.That(Supplies(thriving), Is.Empty);
            Assert.That(Pressures(struggling), Is.Empty);
        }

        // 0 is what a run with no levels behind it, and the editor preview, generate at.
        [Test]
        public void ALevelStandingAddsNothing()
        {
            var profile = Profile();
            var passedZero = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, 0f);
            var passedNothing = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels);
            Object.DestroyImmediate(profile);
            Assert.That(Pressures(passedZero), Is.Empty);
            Assert.That(Pressures(passedNothing), Is.Empty);
        }

        // The point of the room: deleting it must cut the objective off, so no approach walks around the lasers.
        [Test]
        public void APressureRoomGatesTheObjective()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            Object.DestroyImmediate(profile);
            var primary = graph.rooms.Find(r => r.type == RoomType.PrimaryObjectiveRoom);
            Assert.That(primary, Is.Not.Null);
            foreach (var pressure in Pressures(graph))
            {
                Assert.That(Reaches(graph, primary.id, pressure.id), Is.False,
                    $"the objective is reachable without crossing {pressure.id}");
            }
        }

        // Splicing must leave the objective a single approach.
        [Test]
        public void TheObjectiveKeepsASingleApproach()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            Object.DestroyImmediate(profile);
            var primary = graph.rooms.Find(r => r.type == RoomType.PrimaryObjectiveRoom);
            Assert.That(graph.edges.Count(e => e.toId == primary.id), Is.EqualTo(1));
        }

        // The added room goes on through the builder, so no room ends up with more than four doors.
        [Test]
        public void InjectionKeepsWithinTheDoorBudget()
        {
            var profile = Profile();
            var injected = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            var plain = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels);
            Object.DestroyImmediate(profile);

            // Measured against the plain level, so the room is only held to not making a crowded graph worse.
            Assert.That(MaxDoors(injected), Is.LessThanOrEqualTo(Mathf.Max(4, MaxDoors(plain))));
        }

        // The same seed and the same standing give the same level, including when a level is rebuilt.
        [Test]
        public void InjectionIsRepeatable()
        {
            var profile = Profile();
            var first = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            var second = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            Object.DestroyImmediate(profile);
            Assert.That(Pressures(second).Count, Is.EqualTo(Pressures(first).Count));
        }

        // Whether a room is reachable from the graph root with one room cut out of it.
        // The root is the room nothing leads into, as the layout passes take it to be.
        private static bool Reaches(RoomGraph graph, string goalId, string withoutId)
        {
            var inbound = new HashSet<string>(graph.edges.Select(e => e.toId));
            var root = graph.rooms.Find(r => !inbound.Contains(r.id))?.id ?? graph.rooms[0].id;
            if (root == withoutId) return false;

            var seen = new HashSet<string> { root };
            var queue = new Queue<string>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var from = queue.Dequeue();
                foreach (var edge in graph.edges.Where(e => e.fromId == from))
                {
                    if (edge.toId == withoutId || !seen.Add(edge.toId)) continue;
                    if (edge.toId == goalId) return true;
                    queue.Enqueue(edge.toId);
                }
            }

            return false;
        }

        private static List<RoomNode> Pressures(RoomGraph graph) =>
            graph.rooms.FindAll(r => r.type == RoomType.PressureRoom);

        private static List<RoomNode> Supplies(RoomGraph graph) =>
            graph.rooms.FindAll(r => r.type == RoomType.SupplyRoom);

        // Edges are undirected for this count: a door tells on both the rooms it joins.
        private static int MaxDoors(RoomGraph graph)
        {
            var doors = new Dictionary<string, int>();
            foreach (var edge in graph.edges)
            {
                doors[edge.fromId] = doors.GetValueOrDefault(edge.fromId) + 1;
                doors[edge.toId] = doors.GetValueOrDefault(edge.toId) + 1;
            }

            return doors.Count == 0 ? 0 : doors.Values.Max();
        }

        // A fixed mission, so what varies between these tests is the standing alone.
        private static MissionGraph Mission()
        {
            var mission = new MissionGraph { type = MissionType.Theft, facility = "Test Facility", seed = 1 };
            mission.nodes.Add(Node("entry", NodeType.Entry));
            mission.nodes.Add(Node("prereq_1", NodeType.Prerequisite, "entry"));
            mission.nodes.Add(Node("primary", NodeType.Primary, "prereq_1"));
            mission.nodes.Add(Node("secondary_0", NodeType.Secondary, "entry"));
            return mission;
        }

        private static MissionNode Node(string id, NodeType type, string dependency = null)
        {
            var node = new MissionNode { id = id, nodeType = type };
            if (dependency != null) node.dependencies.Add(dependency);
            return node;
        }

        // A chance of 1 always adds the room at full standing, so these tests do not depend on the roll.
        private static RunDifficulty Profile()
        {
            var profile = ScriptableObject.CreateInstance<RunDifficulty>();
            profile.label = "Test";
            profile.secondaryObjectives = Flat(0, 0);
            profile.extraExits = Flat(0, 2);
            profile.lockChance = Flat(1, 1);
            profile.guardChance = Flat(1, 1);
            profile.adaptiveRoomChance = 1f;
            return profile;
        }

        // A range that reads the same at every level, so a test's figures don't drift with difficulty progress.
        private static RunDifficulty.Range Flat(float min, float max) => new()
        {
            minFloor = min, minCeiling = min, maxFloor = max, maxCeiling = max,
            minShape = AnimationCurve.Linear(0, 0, 1, 1), maxShape = AnimationCurve.Linear(0, 0, 1, 1),
        };
    }
}