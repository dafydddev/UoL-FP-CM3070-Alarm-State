using System.Collections.Generic;
using Entities;
using Generation;
using Generation.Lasers;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Fits each pressure room with its laser grid.
    // Whether a level has one is decided upstream by the room graph generator's adaptive pass.
    public class PressureRoomSpawner : EntitySpawner
    {
        [SerializeField] private GameObject laserGridPrefab;
        [SerializeField, Min(2)] private int lasersPerRoom = 4;
        [SerializeField, Min(4)] private int cyclePeriod = 16;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Seeded from the level and drawn in room order, so a rebuilt level lays out the same grids.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Lasers, graph.level));
            var period = Mathf.Max(4, cyclePeriod - cyclePeriod % 2); // an odd period has no equal halves

            foreach (var room in graph.rooms)
            {
                if (room.type != RoomType.PressureRoom) continue;
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(laserGridPrefab, pos, Quaternion.identity, transform);
                go.name = $"Lasers_{room.id}";
                var grid = go.GetComponent<LaserGrid>();
                grid.Init(world, rect, LaserGridLayout.For(rect, lasersPerRoom, period, rng), period);
            }
        }
    }
}