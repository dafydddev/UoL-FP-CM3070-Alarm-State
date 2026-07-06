using Generation.Missions;
using Generation.Rooms;
using Generation.Spawners;
using Simulation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Layout
{
    // Top-level level builder. Runs the full generation pipeline in order:
    // mission -> room -> layout -> realise tiles -> paint -> spawn props (player, items, etc.).
    [RequireComponent(typeof(MissionGenerator))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        [SerializeField] private Scheduler scheduler;

        [SerializeField] private DifficultyProfile profile; // difficulty curves shared by the whole pipeline
        [SerializeField, Min(1)] private int level = 1; // current level within the run, feeds difficulty scaling
        [SerializeField, Min(1)] private int totalLevels = 10; // total run length

        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Tileset tileset;

        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private KeycardSpawner keycardSpawner;
        [SerializeField] private LockedDoorSpawner lockedDoorSpawner;
        [SerializeField] private ObjectiveSpawner objectiveSpawner;
        [SerializeField] private ExitSpawner exitSpawner;
        [SerializeField] private CoverSpawner coverSpawner;

        // The walkable grid the actors query, rebuilt each generation.
        public FacilityGrid Grid { get; private set; }

        private void Awake() => Generate();

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
            var roles = TiledLayoutGenerator.Generate(rooms, out var rects);

            // Realise each role into a tile: keep it for queries and paint it onto the tilemap.
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

            Grid = new FacilityGrid(tiles);

            // Tint rooms by role for readability.
            RoomColourCoder.Apply(tilemap, rooms, rects);

            // Spawn the player at the centre of the entrance room.
            var e = rects["room_entrance"];
            var playerSpawnPos = tilemap.GetCellCenterWorld(new Vector3Int(e.CenterX, e.CenterY, 0));
            playerSpawner?.SpawnPlayer(playerSpawnPos, Grid, tilemap, scheduler);

            // Populate the rest of the level.
            keycardSpawner?.Spawn(rooms, rects, tilemap);
            lockedDoorSpawner?.Spawn(rooms, rects, tilemap);
            objectiveSpawner?.Spawn(rooms, rects, tilemap);
            exitSpawner?.Spawn(rooms, rects, tilemap);
            coverSpawner?.Spawn(rooms, rects, tilemap);

            // objectiveTracker.Init(rooms, mission);
            // guardSpawner.Spawn(rooms, rects, tilemap, navigation);
            // distractionSpawner.Spawn(rooms, rects, tilemap);
            // disguiseSpawner.Spawn(rooms, rects, tilemap);
        }

        // Destroys everything spawned under each spawner from the previous level.
        private void ClearSpawners()
        {
            playerSpawner?.ClearPlayer();
            keycardSpawner?.ClearChildren();
            lockedDoorSpawner?.ClearChildren();
            exitSpawner?.ClearChildren();
            objectiveSpawner?.ClearChildren();
            coverSpawner?.ClearChildren();
        }
    }
}