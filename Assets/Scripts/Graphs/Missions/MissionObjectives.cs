using System.Collections.Generic;

namespace Graphs.Missions
{
    // Static content pool the generator draws from: facility names and, per mission type,
    // the candidate prerequisite chains, optional secondaries, and the final objective text.
    public static class MissionObjectives
    {
        // Facility names picked at random for flavour.
        public static readonly string[] Facilities =
        {
            "Secure Research Complex", "Military Installation", "Corporate Headquarters",
            "Embassy Annex", "Industrial Compound", "Offshore Platform", "Government Archive"
        };

        // Per mission type: alternative prerequisite chains, optional secondary objectives,
        // and the text/label for the terminal (primary) objective.
        public static readonly Dictionary<MissionType, (NodeData[][] prereqSets, NodeData[] secondaries,
            string terminalText, string terminalLabel)> Data = new()
        {
            [MissionType.Assassination] = (
                // prereqSets: one of these chains is chosen; each entry becomes a prerequisite step.
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate target", "Identify location") },
                    new[]
                    {
                        new NodeData("Disable cameras", "Remove surveillance"),
                        new NodeData("Acquire access card", "Enter restricted area")
                    },
                    new[] { new NodeData("Obtain keycard", "Access target wing") },
                    new[]
                    {
                        new NodeData("Intercept schedule", "Find patrol window"),
                        new NodeData("Disable alarm", "Prevent alert")
                    }
                },
                // secondaries: optional objectives, a random subset is added.
                secondaries: new[]
                {
                    new NodeData("Steal documents", "Optional: intelligence"),
                    new NodeData("Plant evidence", "Optional: misdirection"),
                    new NodeData("Photograph facility", "Optional: reconnaissance"),
                    new NodeData("Sabotage generator", "Optional: power outage"),
                    new NodeData("Copy hard drive", "Optional: data extraction")
                },
                terminalText: "Eliminate target", terminalLabel: "Primary objective"
            ),
            [MissionType.Theft] = (
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate asset", "Find storage room") },
                    new[]
                    {
                        new NodeData("Crack safe code", "Bypass security"),
                        new NodeData("Acquire keycard", "Access vault")
                    },
                    new[] { new NodeData("Disable weight sensor", "Bypass trap") },
                    new[]
                    {
                        new NodeData("Clone access badge", "Impersonate staff"),
                        new NodeData("Disable motion sensors", "Clear detection")
                    }
                },
                secondaries: new[]
                {
                    new NodeData("Photograph blueprints", "Optional: intelligence"),
                    new NodeData("Plant tracker", "Optional: surveillance"),
                    new NodeData("Swap decoy", "Optional: delay discovery"),
                    new NodeData("Steal credentials", "Optional: future access"),
                    new NodeData("Copy encryption key", "Optional: data access")
                },
                terminalText: "Extract asset", terminalLabel: "Primary objective"
            ),
            [MissionType.Sabotage] = (
                prereqSets: new[]
                {
                    new[] { new NodeData("Locate control room", "Find system access") },
                    new[]
                    {
                        new NodeData("Obtain access codes", "Bypass lock"),
                        new NodeData("Disable fire suppression", "Prevent auto-repair")
                    },
                    new[] { new NodeData("Cut comms relay", "Prevent reinforcements") },
                    new[]
                    {
                        new NodeData("Acquire explosives", "Collect charges"),
                        new NodeData("Map blast radius", "Ensure safe exit")
                    }
                },
                secondaries: new[]
                {
                    new NodeData("Download schematics", "Optional: intelligence"),
                    new NodeData("Destroy backups", "Optional: no recovery"),
                    new NodeData("Eliminate engineer", "Optional: delay repair"),
                    new NodeData("Disable backup power", "Optional: extend outage"),
                    new NodeData("Steal prototype", "Optional: extra objective")
                },
                terminalText: "Destroy target system", terminalLabel: "Primary objective"
            )
        };
    }
}