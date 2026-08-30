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
    // The adaptive pass: a struggling player is given a supply room, a thriving one is not.
    public class RoomGraphAdaptiveTests
    {
        private const int TotalLevels = 20;
        private const int Level = 10;
        private const float Struggling = -1f;
        private const float Thriving = 1f;

        [Test]
        public void AStrugglingPlayerGetsASupplyRoom()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            Object.DestroyImmediate(profile);

            Assert.That(Supplies(graph), Is.Not.Empty, "a struggling player should get a supply room");
        }

        [Test]
        public void AThrivingPlayerGetsNoSupplyRoom()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Thriving);
            Object.DestroyImmediate(profile);

            Assert.That(Supplies(graph), Is.Empty);
        }

        // Zero is what a run inside the cooldown reports, and it is also the default.
        [Test]
        public void ALevelStandingAddsNothing()
        {
            var profile = Profile();
            var passedZero = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, 0f);
            var passedNothing = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels);
            Object.DestroyImmediate(profile);

            Assert.That(Supplies(passedZero), Is.Empty);
            Assert.That(Supplies(passedNothing), Is.Empty);
        }

        // The supply room hangs off a corridor or side objective and leads nowhere, so visiting it is optional.
        [Test]
        public void ASupplyRoomIsALeafOffTheCriticalPath()
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            Object.DestroyImmediate(profile);

            var parents = new Dictionary<string, string>();
            foreach (var edge in graph.edges)
            {
                parents[edge.toId] = edge.fromId;
            }

            foreach (var supply in Supplies(graph))
            {
                Assert.That(graph.edges.Any(e => e.fromId == supply.id), Is.False,
                    $"{supply.id} leads onward instead of being a dead end");
                Assert.That(graph.edges.Any(e => e.toId == supply.id && e.locked), Is.False,
                    $"{supply.id} is locked, so its health pack may be unreachable");

                Assert.That(parents.TryGetValue(supply.id, out var parentId), Is.True, $"{supply.id} has no approach");
                var parent = graph.GetRoom(parentId);
                Assert.That(parent, Is.Not.Null);
                Assert.That(parent.type,
                    Is.EqualTo(RoomType.Corridor).Or.EqualTo(RoomType.SecondaryObjectiveRoom),
                    $"{supply.id} hangs off {parent.type}, which can be on the route through the level");
            }
        }

        // The added room goes on through Attach, so no room ends up with more than four doors.
        [Test]
        public void InjectionKeepsWithinTheDoorBudget()
        {
            var profile = Profile();
            var injected = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
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
            var first = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            var second = RoomGraphGenerator.Generate(Mission(), profile, Level, TotalLevels, Struggling);
            Object.DestroyImmediate(profile);

            Assert.That(Supplies(second).Count, Is.EqualTo(Supplies(first).Count));
        }

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

        // A fixed mission, so what varies between these tests is just the standing.
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