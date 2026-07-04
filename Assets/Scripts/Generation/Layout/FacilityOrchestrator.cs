using Generation.Missions;
using Generation.Rooms;
using UnityEngine;

namespace Generation.Layout
{
    // Top-level level builder. Runs the generation pipeline in order:
    // mission -> room graph -> tile grid -> 3D build.
    // As spawner/navigation/disguise systems are added, they hook in after Build().
    [RequireComponent(typeof(MissionGenerator))]
    [RequireComponent(typeof(FacilityLayoutBuilder))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        public DifficultyProfile profile;
        public int level;

        private void Start()
        {
            Generate();
        }

        // Builds a complete level. Also exposed in the inspector's context menu for quick re-testing.
        [ContextMenu("Generate")]
        public void Generate()
        {
            // Generate the mission, expand it into a room graph, then into a tile grid.
            var mission = GetComponent<MissionGenerator>().Generate();
            var rooms = RoomGraphGenerator.Generate(mission, profile, level);
            // room rects (out _) will be needed once spawners are added on top of the layout.
            var grid = TiledLayoutGenerator.Generate(rooms, out _);

            // Build the grid into the 3D scene.
            GetComponent<FacilityLayoutBuilder>().Build(grid);
        }
    }
}
