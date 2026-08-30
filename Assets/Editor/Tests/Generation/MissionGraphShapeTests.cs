using System.Collections.Generic;
using System.Linq;
using Graphs;
using Graphs.Missions;
using NUnit.Framework;
using Run;
using UnityEngine;

namespace Editor.Tests.Generation
{
    // Shape of the generated mission graph: one entry, one terminal primary, and dependencies that point backwards.
    public class MissionGraphShapeTests
    {
        private const int TotalLevels = 20;

        [Test]
        public void ThereIsExactlyOneEntryAndOnePrimary([Values(1, 7, 42)] int seed)
        {
            var mission = GenerateMission(seed);
            Assert.That(mission.nodes.Count(n => n.nodeType == NodeType.Entry), Is.EqualTo(1));
            Assert.That(mission.nodes.Count(n => n.nodeType == NodeType.Primary), Is.EqualTo(1));
        }

        [Test]
        public void TheEntryHasNoDependencies([Values(1, 7, 42)] int seed)
        {
            var entry = GenerateMission(seed).nodes.First(n => n.nodeType == NodeType.Entry);

            Assert.That(entry.dependencies, Is.Empty);
        }

        // Every dependency resolves to a node created earlier, so the graph cannot contain a cycle.
        [Test]
        public void EveryDependencyPointsAtAnEarlierNode([Values(1, 7, 42)] int seed)
        {
            var mission = GenerateMission(seed);
            var index = new Dictionary<string, int>();
            for (var i = 0; i < mission.nodes.Count; i++)
            {
                index[mission.nodes[i].id] = i;
            }

            for (var i = 0; i < mission.nodes.Count; i++)
            {
                foreach (var dependency in mission.nodes[i].dependencies)
                {
                    Assert.That(index, Contains.Key(dependency), $"unknown dependency \"{dependency}\"");
                    Assert.That(index[dependency], Is.LessThan(i),
                        $"{mission.nodes[i].id} depends on {dependency}, which comes later");
                }
            }
        }

        [Test]
        public void EveryPrerequisiteHasExactlyOneDependency([Values(1, 7, 42)] int seed)
        {
            foreach (var prerequisite in GenerateMission(seed).nodes.Where(n => n.nodeType == NodeType.Prerequisite))
            {
                Assert.That(prerequisite.dependencies.Count, Is.EqualTo(1), $"{prerequisite.id} is not a chain link");
            }
        }

        [Test]
        public void SecondariesBranchOffTheEntryOrAPrerequisite([Values(1, 7, 42)] int seed)
        {
            var mission = GenerateMission(seed);
            var byId = mission.nodes.ToDictionary(n => n.id);
            var secondaries = mission.nodes.Where(n => n.nodeType == NodeType.Secondary).ToList();

            Assert.That(secondaries, Is.Not.Empty, "the profile should request secondary objectives");

            foreach (var secondary in secondaries)
            {
                Assert.That(secondary.dependencies.Count, Is.EqualTo(1));
                Assert.That(byId[secondary.dependencies[0]].nodeType,
                    Is.EqualTo(NodeType.Entry).Or.EqualTo(NodeType.Prerequisite),
                    $"{secondary.id} branches off a node it should not");
            }
        }

        // Secondaries are drawn from the pool without replacement.
        [Test]
        public void NoSecondaryObjectiveAppearsTwice([Values(1, 7, 42)] int seed)
        {
            var texts = GenerateMission(seed).nodes
                .Where(n => n.nodeType == NodeType.Secondary)
                .Select(n => n.text)
                .ToList();

            Assert.That(texts, Is.Unique);
        }

        [Test]
        public void TheSameSeedProducesTheSameMission()
        {
            var first = GenerateMission(99);
            var second = GenerateMission(99);

            Assert.That(second.type, Is.EqualTo(first.type));
            Assert.That(second.facility, Is.EqualTo(first.facility));
            Assert.That(Describe(second), Is.EqualTo(Describe(first)));
        }

        // Flattens the graph to one string, so a single comparison covers ids, types, wording and dependencies.
        private static string Describe(MissionGraph mission) =>
            string.Join("|",
                mission.nodes.Select(n => $"{n.id}:{n.nodeType}:{n.text}:{string.Join(",", n.dependencies)}"));

        // The generator is a MonoBehaviour, so it needs a host object; both it and the profile are torn down after.
        private static MissionGraph GenerateMission(int seed, int level = 1)
        {
            var profile = Profile();
            var host = new GameObject("~MissionGraphShapeTests") { hideFlags = HideFlags.HideAndDontSave };
            var generator = host.AddComponent<MissionGenerator>();
            generator.randomSeed = false; // the seed is the tests, not a fresh one per call
            generator.randomType = true;
            generator.seed = seed;

            var mission = generator.Generate(profile, level, TotalLevels);

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(profile);
            return mission;
        }

        // Two secondaries every time, so the branching and no-repeat rules always have something to check.
        private static RunDifficulty Profile()
        {
            var profile = ScriptableObject.CreateInstance<RunDifficulty>();
            profile.label = "Test";
            profile.secondaryObjectives = Flat(2, 2);
            profile.extraExits = Flat(0, 0);
            profile.lockChance = Flat(1, 1);
            profile.guardChance = Flat(1, 1);
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