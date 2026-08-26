using System.Collections.Generic;
using System.Linq;
using Entities.Items;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Spawns a cache of health pickups in each supply room.
    // Whether a level has one is decided upstream by the room graph generator's adaptive pass.
    public class SupplyRoomSpawner : EntitySpawner
    {
        [SerializeField] private GameObject healthPrefab;
        [SerializeField, Min(1)] private int packsPerRoom = 3;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            if (!healthPrefab) return;

            var placed = 0;
            foreach (var room in graph.rooms.Where(room => room.type == RoomType.SupplyRoom))
            {
                if (!rects.TryGetValue(room.id, out var rect)) continue;

                var cells = FreeCells(world, rect);
                var packs = Mathf.Min(packsPerRoom, cells.Count);
                for (var i = 0; i < packs; i++)
                {
                    var cell = cells[i];
                    var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                    var go = Instantiate(healthPrefab, pos, Quaternion.identity, transform);
                    var pickup = go.GetComponent<HealthPickup>();
                    pickup.healthPickupId = $"supply_{placed}";
                    pickup.Init(world);
                    go.name = $"Supply_{room.id}_{i}";
                    placed++;
                }
            }
        }

        private static List<Vector2Int> FreeCells(WorldContext world, RoomRect rect)
        {
            var cells = new List<Vector2Int>();
            for (var x = rect.X + 1; x < rect.Right - 1; x++)
            {
                for (var y = rect.Y + 1; y < rect.Bottom - 1; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var tile = world.Grid.At(cell);
                    if (tile && !tile.BlocksEntry(null) && !world.Occupancy.At(cell)) cells.Add(cell);
                }
            }

            var centre = new Vector2Int(rect.CenterX, rect.CenterY);
            cells.Sort((a, b) => (a - centre).sqrMagnitude.CompareTo((b - centre).sqrMagnitude));
            return cells;
        }
    }
}