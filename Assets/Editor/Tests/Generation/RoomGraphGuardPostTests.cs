using System.Collections.Generic;
using Graphs;
using Graphs.Missions;
using Graphs.Rooms;
using NUnit.Framework;
using Run;
using UnityEngine;

namespace Editor.Tests.Generation
{
    // Guard posts in front of objective rooms: no approach may bypass them.
    public class RoomGraphGuardPostTests
    {
        private const int TotalLevels = 20;

        [Test]
        public void EveryObjectiveRoomIsEnteredThroughAGuardPost([Values(1, 10, 20)] int level)
        {
            var profile = Profile();
            var graph = RoomGraphGenerator.Generate(ObjectiveMission(), profile, level, TotalLevels);
            Object.DestroyImmediate(profile);

            var parents = ParentLookup(graph);
            var objectives = graph.rooms.FindAll(r => r.type.IsObjective());
            Assert.That(objectives, Is.Not.Empty, "the mission should produce at least one objective room");

            foreach (var objective in objectives)
            {
                var approach = FirstNonCorridorAncestor(graph, parents, objective.id);
                Assert.That(approach, Is.Not.Null, $"{objective.id} has no room guarding its approach");
                Assert.That(approach.type, Is.EqualTo(RoomType.GuardPost),
                    $"{objective.id} can be entered from {approach.id} ({approach.type}) without passing a guard post.");
            }
        }

        // Each room's parent, from the edge that enters it.
        private static Dictionary<string, string> ParentLookup(RoomGraph graph)
        {
            var parents = new Dictionary<string, string>();
            foreach (var edge in graph.edges) parents[edge.toId] = edge.fromId;
            return parents;
        }

        // Walks up the parent chain, stepping over the corridors SpliceCorridors inserts.
        private static RoomNode FirstNonCorridorAncestor(RoomGraph graph,
            Dictionary<string, string> parents, string roomId)
        {
            var seen = new HashSet<string> { roomId };
            while (parents.TryGetValue(roomId, out var parentId) && seen.Add(parentId))
            {
                var parent = graph.GetRoom(parentId);
                if (parent == null) return null;
                if (parent.type != RoomType.Corridor) return parent;
                roomId = parentId;
            }

            return null;
        }

        // A mission with both objective kinds: a primary behind a prerequisite, and a secondary off the entry.
        private static MissionGraph ObjectiveMission()
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

        private static RunDifficulty Profile()
        {
            var profile = ScriptableObject.CreateInstance<RunDifficulty>();
            profile.label = "Test";
            profile.secondaryObjectives = Flat(0, 0);
            profile.extraExits = Flat(0, 2);
            profile.lockChance = Flat(1, 1);
            profile.guardChance = Flat(1, 1);
            return profile;
        }

        private static RunDifficulty.Range Flat(float min, float max) => new()
        {
            minFloor = min, minCeiling = min, maxFloor = max, maxCeiling = max,
            minShape = AnimationCurve.Linear(0, 0, 1, 1), maxShape = AnimationCurve.Linear(0, 0, 1, 1),
        };
    }
}
