using Generation.Tiles;
using Graphs.Missions;
using Graphs.Rooms;
using Simulation;
using Spawners;
using UnityEngine;

namespace Generation.Facility
{
    // Top-level level builder. Runs the full generation pipeline in order:
    // mission -> room -> layout -> realise tiles -> paint -> spawn props (player, items, etc.).
    [RequireComponent(typeof(MissionGenerator))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Tilemaps.Tilemap tilemap;
        [SerializeField] private Scheduler scheduler;
        [SerializeField] private SimulationClock clock;

        [SerializeField] private DifficultyProfile profile; // difficulty curves shared by the whole pipeline
        [SerializeField, Min(1)] private int level = 1; // current level within the run, feeds difficulty scaling
        [SerializeField, Min(1)] private int totalLevels = 10; // total run length

        [SerializeField] private Tileset tileset;

        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private KeycardSpawner keycardSpawner;
        [SerializeField] private LockedDoorSpawner lockedDoorSpawner;
        [SerializeField] private ObjectiveSpawner objectiveSpawner;
        [SerializeField] private ExitSpawner exitSpawner;
        [SerializeField] private CoverSpawner coverSpawner;

        // The context for the current level, rebuilt on every Generate.
        public WorldContext World { get; private set; }

        private void Start()
        {
            Generate();
        }

        [ContextMenu("Clear")]
        private void ClearFacility()
        {
            if (tilemap) tilemap.ClearAllTiles();
            ClearSpawners();
        }

        // Builds a complete level. Exposed in the inspector's context menu for quick testing.
        [ContextMenu("Generate")]
        public void Generate()
        {
            // Remove anything spawned by a previous run.
            ClearFacility();

            // Generate the mission, expand it into a room graph, then into a structural grid.
            var mission = GetComponent<MissionGenerator>().Generate(profile, level, totalLevels);
            var rooms = RoomGraphGenerator.Generate(mission, profile, level, totalLevels);
            var roles = FacilityTiledLayoutGenerator.Generate(rooms, out var rects);

            // Realise each role into a tile: keep it in the grid for queries and paint it.
            var gridW = roles.GetLength(0);
            var gridH = roles.GetLength(1);
            var tiles = new TileDefinition[gridW, gridH];
            for (var x = 0; x < gridW; x++)
            for (var y = 0; y < gridH; y++)
            {
                var tile = tileset.For(roles[x, y]);
                tiles[x, y] = tile;
                if (tile) tilemap.SetTile(new Vector3Int(x, y, 0), tile.TileBase);
            }

            // Wire up the fresh level context that everything spawned below receives.
            World = new WorldContext(tilemap, scheduler, clock, new FacilityGrid(tiles));

            // Tint rooms by role for readability.
            FacilityColourCoder.Apply(tilemap, rooms, rects);

            // Populate the level: sim participants get the world, set dressing just the tilemap.
            playerSpawner?.Spawn(rooms, rects, World);
            keycardSpawner?.Spawn(rooms, rects, World);
            lockedDoorSpawner?.Spawn(rooms, rects, World);
            objectiveSpawner?.Spawn(rooms, rects, tilemap);
            exitSpawner?.Spawn(rooms, rects, tilemap);
            coverSpawner?.Spawn(rooms, rects, tilemap);
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
        }
    }
}