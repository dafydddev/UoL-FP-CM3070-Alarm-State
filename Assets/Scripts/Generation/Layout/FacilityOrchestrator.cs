using Generation.Missions;
using Generation.Rooms;
using Generation.Spawners;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Layout
{
    // Top-level level builder. Runs the full generation pipeline in order:
    // mission -> room -> graph -> tile layout -> paint tiles -> spawn props (e.g. player, items, guard, etc.).
    [RequireComponent(typeof(MissionGenerator))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        [SerializeField] private DifficultyProfile profile; // difficulty curves shared by the whole pipeline
        [SerializeField, Min(1)] private int level = 1; // current level within the run, feeds difficulty scaling
        [SerializeField, Min(1)] private int totalLevels = 10; // total run length

        // Tilemap and tile assets used to paint the grid.
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private TileBase floorTile;
        [SerializeField] private TileBase wallTile;
        [SerializeField] private TileBase doorTile;

        // The per-system spawners/services this orchestrator drives.
        [SerializeField] private PlayerSpawner playerSpawner;

        [SerializeField] private KeycardSpawner keycardSpawner;
        [SerializeField] private LockedDoorSpawner lockedDoorSpawner;
        // public ObjectiveSpawner objectiveSpawner;
        // public ObjectiveTracker objectiveTracker;
        // public ExitSpawner exitSpawner;
        // public FacilityNavigation navigation;
        // public GuardSpawner guardSpawner;
        // public DistractionSpawner distractionSpawner;
        // public CoverSpawner coverSpawner;
        // public DisguiseSpawner disguiseSpawner;

        private void Awake() => Generate();

        // Builds a complete level. Exposed in the inspector's context menu for quick testing.
        [ContextMenu("Generate")]
        public void Generate()
        {
            // Remove anything spawned by a previous run.
            ClearSpawned();

            // Generate the mission, expand it into a room graph, then into a tile grid.
            // The orchestrator owns the difficulty profile and hands it to each stage.
            var mission = GetComponent<MissionGenerator>().Generate(profile, level, totalLevels);
            var rooms = RoomGraphGenerator.Generate(mission, profile, level, totalLevels);
            var grid = TiledLayoutGenerator.Generate(rooms, out var rects);

            // Paint the grid onto the tilemap.
            tilemap.ClearAllTiles();
            for (var x = 0; x < grid.GetLength(0); x++)
            {
                for (var y = 0; y < grid.GetLength(1); y++)
                {
                    var tile = grid[x, y] switch
                    {
                        TileType.Floor => floorTile,
                        TileType.Wall => wallTile,
                        TileType.Door => doorTile,
                        _ => null
                    };
                    if (tile) tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            // Tint rooms by role for readability.
            RoomColourCoder.Apply(tilemap, rooms, rects);

            // Spawn the player at the centre of the entrance room.
            var e = rects["room_entrance"];
            var playerSpawnPos = tilemap.GetCellCenterWorld(new Vector3Int(e.CenterX, e.CenterY, 0)); 
            playerSpawner?.Spawn(playerSpawnPos);

            // Populate the rest of the level. Navigation must be built before guards, which need it.
            keycardSpawner?.Spawn(rooms, rects, tilemap); 
            lockedDoorSpawner?.Spawn(rooms, rects, tilemap);
            // objectiveSpawner.Spawn(rooms, rects, tilemap);
            // objectiveTracker.Init(rooms, mission);
            // exitSpawner.Spawn(rooms, rects, tilemap);
            // navigation.Build(grid);
            // guardSpawner.Spawn(rooms, rects, tilemap, navigation);
            // distractionSpawner.Spawn(rooms, rects, tilemap);
            // coverSpawner.Spawn(rooms, rects, tilemap);
            // disguiseSpawner.Spawn(rooms, rects, tilemap);

            // Reset the static distraction list so stale entries don't carry over.
            // DistractionItem.Clear();
        }

        // Destroys everything spawned under each spawner from the previous level.
        private void ClearSpawned()
        {
            playerSpawner?.ClearPlayer();
            keycardSpawner?.ClearChildren();
            lockedDoorSpawner?.ClearChildren();
            // ClearChildren(objectiveSpawner.transform);
            // ClearChildren(exitSpawner.transform);
            // ClearChildren(guardSpawner.transform);
            // ClearChildren(distractionSpawner.transform);
            // ClearChildren(coverSpawner.transform);
            // ClearChildren(disguiseSpawner.transform);
        }
    }
}