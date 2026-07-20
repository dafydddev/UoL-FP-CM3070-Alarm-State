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
    // Lock-and-key placement in the generated room graph.
    // A locked door's keycard room must be reachable from the entrance without passing through the door that key opens.
    public class RoomGraphKeyPlacementTests
    {
        private const int TotalLevels = 20;

        [Test]
        public void EveryLockedDoorHasItsKeyReachableWithoutOpeningIt([Values(1, 10, 20)] int level)
        {
            var profile = LockEverything();
            var graph = RoomGraphGenerator.Generate(ChainMission(), profile, level, TotalLevels);
            Object.DestroyImmediate(profile);

            var lockedDoors = graph.edges.FindAll(e => e.locked);
            Assert.That(lockedDoors, Is.Not.Empty, "the profile should force at least one locked door");

            foreach (var door in lockedDoors)
            {
                Assert.That(door.keyRoomId, Is.Not.Null,
                    $"locked door {door.fromId} -> {door.toId} has no key room");
                Assert.That(graph.GetRoom(door.keyRoomId), Is.Not.Null,
                    $"key room {door.keyRoomId} is missing from the graph");

                Assert.IsTrue(ReachableFromEntrance(graph, door).Contains(door.keyRoomId),
                    $"key {door.keyRoomId} sits behind the door it opens ({door.fromId} -> {door.toId}).");
            }
        }

        // Rooms reachable from the entrance by following edges, with the given door treated as shut.
        private static HashSet<string> ReachableFromEntrance(RoomGraph graph, RoomEdge shut)
        {
            var children = new Dictionary<string, List<string>>();
            foreach (var edge in graph.edges.Where(edge => edge != shut))
            {
                if (!children.TryGetValue(edge.fromId, out var kids)) children[edge.fromId] = kids = new List<string>();
                kids.Add(edge.toId);
            }

            var seen = new HashSet<string> { "room_entrance" };
            var queue = new Queue<string>(seen);
            while (queue.Count > 0)
            {
                if (!children.TryGetValue(queue.Dequeue(), out var kids)) continue;
                foreach (var kid in kids.Where(seen.Add))
                {
                    queue.Enqueue(kid);
                }
            }

            return seen;
        }

        // entry -> prerequisite -> primary: the smallest mission with a lockable objective door.
        private static MissionGraph ChainMission()
        {
            var mission = new MissionGraph { type = MissionType.Theft, facility = "Test Facility", seed = 1 };
            mission.nodes.Add(Node("entry", NodeType.Entry));
            mission.nodes.Add(Node("prereq_1", NodeType.Prerequisite, "entry"));
            mission.nodes.Add(Node("primary", NodeType.Primary, "prereq_1"));
            return mission;
        }

        private static MissionNode Node(string id, NodeType type, string dependency = null)
        {
            var node = new MissionNode { id = id, nodeType = type };
            if (dependency != null) node.dependencies.Add(dependency);
            return node;
        }

        // Forces every objective door to lock and every keycard room to be guarded.
        private static RunDifficulty LockEverything()
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