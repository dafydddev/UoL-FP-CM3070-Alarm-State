using Camera;
using Generation.Terrain;
using Generation.Tiles;
using Graphs.Missions;
using Graphs.Rooms;
using Hacking;
using MiniMap;
using Run;
using Simulation;
using Spawners;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Facility
{
    // Top-level level builder. Runs the full generation pipeline in order:
    // mission -> room -> layout -> tiles -> paint -> spawn entites (player, items, etc.).
    [RequireComponent(typeof(MissionGenerator))]
    [RequireComponent(typeof(ExteriorGenerator))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;

        [SerializeField] private Scheduler scheduler;
        [SerializeField] private SimulationClock clock;

        [SerializeField] private RunDifficulty profile;

        [SerializeField] private MinimapFramer minimap;
        [SerializeField] private HackingMinigame hackingMinigame;
        [SerializeField] private Tileset tileset;

        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private KeycardSpawner keycardSpawner;
        [SerializeField] private LockedDoorSpawner lockedDoorSpawner;
        [SerializeField] private ObjectiveSpawner objectiveSpawner;
        [SerializeField] private ExitSpawner exitSpawner;
        [SerializeField] private CoverSpawner coverSpawner;
        [SerializeField] private DistractionSpawner distractionSpawner;
        [SerializeField] private GuardSpawner guardSpawner;
        
        [SerializeField] private int previewLevel = 1;
        [SerializeField] private int previewTotalLevels = 20; // run length to preview at; drives difficulty progress
        [SerializeField] private TileLayoutStyle previewLayoutStyle = TileLayoutStyle.Spine;

        private MissionGenerator _missionGenerator;

        private MissionGenerator MissionGenerator => _missionGenerator ??= GetComponent<MissionGenerator>();

        // The exterior terrain generator lives on the same GameObject, like MissionGenerator.
        private ExteriorGenerator _exteriorGenerator;
        private ExteriorGenerator ExteriorGenerator => _exteriorGenerator ??= GetComponent<ExteriorGenerator>();

        // The context for the current level, rebuilt on every Generate.
        public WorldContext World { get; private set; }

        [ContextMenu("Clear Level")]
        private void ClearFacility()
        {
            if (tilemap) tilemap.ClearAllTiles();
            ExteriorGenerator.Clear();
            ClearSpawners();
        }

        [ContextMenu("Generate Preview")]
        public void GeneratePreview() => Generate(new RunContext(profile, previewLevel, previewTotalLevels, previewLayoutStyle));

        // Builds a complete level using the supplied run state.
        public void Generate(RunContext run)
        {
            // Remove anything spawned by a previous run.
            ClearFacility();

            // Generate the mission, expand it into a room graph, then into a structural grid.
            var mission = MissionGenerator.Generate(run.Profile, run.CurrentLevel, run.TotalLevels);
            var rooms = RoomGraphGenerator.Generate(mission, run.Profile, run.CurrentLevel, run.TotalLevels);
            var roles = TileLayoutGenerator.Generate(rooms, run.LayoutStyle, out var rects);

            // Realise each role into a tile: keep it in the grid for queries and paint it.
            var gridW = roles.GetLength(0);
            var gridH = roles.GetLength(1);
            var tiles = new TileDefinition[gridW, gridH];

            for (var x = 0; x < gridW; x++)
            for (var y = 0; y < gridH; y++)
            {
                var tile = tileset.For(roles[x, y]);
                tiles[x, y] = tile;

                if (tile)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile.TileBase);
            }

            // Wire up the fresh level context that everything spawned below receives.
            World = new WorldContext(tilemap, scheduler, clock, new FacilityGrid(tiles));

            // Tint rooms by role for readability.
            FacilityColourCoder.Apply(tilemap, rooms, rects);
            
            // Generate and paint the exterior terrain behind the facility using PCG noise.
            ExteriorGenerator.Paint(roles, rooms.seed, rooms.level);

            // Populate the level: sim participants get the world, set dressing just the tilemap.
            playerSpawner?.Spawn(rooms, rects, World);
            keycardSpawner?.Spawn(rooms, rects, World);
            lockedDoorSpawner?.Spawn(rooms, rects, World);
            objectiveSpawner?.Spawn(rooms, rects, World);
            exitSpawner?.Spawn(rooms, rects, World);
            coverSpawner?.Spawn(rooms, rects, World);
            distractionSpawner?.Spawn(rooms, rects, World);
            guardSpawner?.Spawn(rooms, rects, World); // after the player, so guards can sense them from the first tick

            // Hand the hacking screen the run state, so its boards scale with the level.
            hackingMinigame?.Prepare(run);
            // Scale the mini-map for the generated level.
            minimap?.Fit();
        }

        // Destroys everything spawned under each spawner from the previous level.
        private void ClearSpawners()
        {
            playerSpawner?.ClearChildren();
            keycardSpawner?.ClearChildren();
            lockedDoorSpawner?.ClearChildren();
            exitSpawner?.ClearChildren();
            objectiveSpawner?.ClearChildren();
            coverSpawner?.ClearChildren();
            distractionSpawner?.ClearChildren();
            guardSpawner?.ClearChildren();
        }
    }
}