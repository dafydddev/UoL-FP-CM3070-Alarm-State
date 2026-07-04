using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Generation.Missions
{
    // Procedurally builds a MissionGraph from the content pool, scaling the number of
    // optional objectives by the difficulty profile and using a seeded RNG for repeatability.
    public class MissionGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        public MissionType forcedType = MissionType.Assassination; // used when randomType is off
        public bool randomType = true;
        public int seed; // used when randomSeed is off
        public bool randomSeed = true;

        private System.Random _rng;

        // Generates a fresh mission graph, scaled by the given difficulty profile.
        public MissionGraph Generate(DifficultyProfile profile, int level, int totalLevels)
        {
            // Resolve and seed the RNG so a given seed always produces the same mission.
            var resolvedSeed = randomSeed ? Random.Range(0, int.MaxValue) : seed;
            _rng = new System.Random(resolvedSeed);

            // Pick the mission type and grab its content set.
            var typeCount = Enum.GetValues(typeof(MissionType)).Length;
            var type = randomType ? (MissionType)_rng.Next(0, typeCount) : forcedType;
            var (prereqSets, secondaries, terminalText, terminalLabel) = MissionObjectives.Data[type];

            // Choose a facility and one of the prerequisite chains.
            var facility = Pick(MissionObjectives.Facilities);
            var prereqSet = Pick(prereqSets);

            var numSecondaries = profile.SecondaryObjectiveCount(level, totalLevels, _rng);

            var graph = new MissionGraph { type = type, facility = facility, seed = resolvedSeed };

            // Start with the entry node.
            var entry = MakeNode("entry", "Infiltrate facility", "Mission start", NodeType.Entry);
            graph.nodes.Add(entry);

            // Chain the prerequisites in order, each depending on the previous step.
            var prevIds = new List<string> { entry.id };
            foreach (var d in prereqSet)
            {
                var node = MakeNode($"prereq_{graph.nodes.Count}", d.text, d.label, NodeType.Prerequisite);
                node.dependencies.AddRange(prevIds);
                graph.nodes.Add(node);
                prevIds = new List<string> { node.id };
            }

            // The primary objective depends on the last prerequisite.
            var terminal = MakeNode("primary", terminalText, terminalLabel, NodeType.Primary);
            terminal.dependencies.AddRange(prevIds);
            graph.nodes.Add(terminal);

            // Secondaries can branch off the entry or any prerequisite node.
            var branchCandidates = graph.nodes
                .FindAll(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite)
                .ConvertAll(n => n.id);

            // Add the chosen number of secondaries, drawing without repeats from the pool.
            var pool = new List<NodeData>(secondaries);
            for (var i = 0; i < numSecondaries && pool.Count > 0; i++)
            {
                var idx = _rng.Next(pool.Count);
                var d = pool[idx];
                pool.RemoveAt(idx);
                var sec = MakeNode($"secondary_{i}", d.text, d.label, NodeType.Secondary);
                sec.dependencies.Add(
                    branchCandidates[_rng.Next(branchCandidates.Count)]); // hang it off a random earlier node
                graph.nodes.Add(sec);
            }

            return graph;
        }

        // Helper to build a node with the given fields.
        private static MissionNode MakeNode(string id, string text, string label, NodeType type) => new()
            { id = id, text = text, label = label, nodeType = type };

        // Picks a random element from an array using the seeded RNG.
        private T Pick<T>(T[] arr) => arr[_rng.Next(arr.Length)];
    }
}