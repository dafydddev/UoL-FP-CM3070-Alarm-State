// Spawner systems not yet ported — restore these usings as each comes back.
// using Disguises;
// using Doors;
// using Exits;
// using Guards;
// using Items;
// using Keycards;
// using Navigation;
// using Objectives;
// using Player;

using Generation.Missions;
using Generation.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Layout
{
    // Top-level level builder. Runs the full generation pipeline in order:
    // mission -> room -> graph -> tile layout -> paint tiles -> spawn props (e.g. player, items, guard, etc.).
    [RequireComponent(typeof(MissionGenerator))]
    public class FacilityOrchestrator : MonoBehaviour
    {
        public DifficultyProfile profile; // difficulty curves shared by the whole pipeline
        [Min(1)] public int level = 1; // current level within the run, feeds difficulty scaling
        [Min(1)] public int totalLevels = 5; // run length, sets how far along the difficulty curves we are

        // Tilemap and tile assets used to paint the grid.
        public Tilemap tilemap;
        public TileBase floorTile;
        public TileBase wallTile;
        public TileBase doorTile;

        // The per-system spawners/services this orchestrator drives.
        // public PlayerSpawner playerSpawner;
        // public KeycardSpawner keycardSpawner;
        // public LockedDoorSpawner lockedDoorSpawner;
        // public ObjectiveSpawner objectiveSpawner;
        // public ObjectiveTracker objectiveTracker;
        // public ExitSpawner exitSpawner;
        // public FacilityNavigation navigation;
        // public GuardSpawner guardSpawner;
        // public DistractionSpawner distractionSpawner;
        // public CoverSpawner coverSpawner;
        // public DisguiseSpawner disguiseSpawner;

        // Builds a complete level. Exposed in the inspector's context menu for quick testing.
        [ContextMenu("Generate")]
        public void Generate()
        {
            // Remove anything spawned by a previous run.
            // ClearSpawned();

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
            // var e = rects["room_entrance"];
            // var spawn = tilemap.GetCellCenterWorld(new Vector3Int(e.CenterX, e.CenterY, 0));
            // playerSpawner.SpawnPlayer(spawn);

            // Populate the rest of the level. Navigation must be built before guards, which need it.
            // keycardSpawner.Spawn(rooms, rects, tilemap);
            // lockedDoorSpawner.Spawn(rooms, rects, tilemap);
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
        // private void ClearSpawned()
        // {
        //     ClearChildren(keycardSpawner.transform);
        //     ClearChildren(lockedDoorSpawner.transform);
        //     ClearChildren(objectiveSpawner.transform);
        //     ClearChildren(exitSpawner.transform);
        //     ClearChildren(guardSpawner.transform);
        //     ClearChildren(distractionSpawner.transform);
        //     ClearChildren(coverSpawner.transform);
        //     ClearChildren(disguiseSpawner.transform);
        // }

        // Destroys all children of a transform, using the play-mode-safe destroy call.
        // private static void ClearChildren(Transform t)
        // {
        //     for (var i = t.childCount - 1; i >= 0; i--)
        //     {
        //         var child = t.GetChild(i).gameObject;
        //         if (Application.isPlaying) Destroy(child);
        //         else DestroyImmediate(child);
        //     }
        // }
    }
}