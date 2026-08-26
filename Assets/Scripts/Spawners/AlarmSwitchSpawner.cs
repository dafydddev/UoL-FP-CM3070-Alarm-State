using System.Collections.Generic;
using System.Linq;
using Entities;
using Generation;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    // Places one alarm switch in each guard post, drawn at random along an interior wall and kept clear of the doorways.
    // Guard posts sit in front of objective and keycard rooms, so switches cluster where intrusions happen.
    public class AlarmSwitchSpawner : EntitySpawner
    {
        [SerializeField] private GameObject alarmSwitchPrefab;

        private const int DoorClearance = 3; // cells a switch must keep away from any doorway

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            // Seed from the graph so switch placement is repeatable per level.
            var rng = new System.Random(Seeds.For(graph.seed, Seeds.Alarm, graph.level));
            foreach (var room in graph.rooms.Where(room => room.type == RoomType.GuardPost))
            {
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                var cell = WallPlacement.Pick(world, rect, rng, DoorClearance);
                var pos = world.Tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                var go = Instantiate(alarmSwitchPrefab, pos, Quaternion.identity, transform);
                go.name = $"AlarmSwitch_{room.id}";
                go.GetComponent<AlarmSwitch>().Init(world);
            }
        }
    }
}