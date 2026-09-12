using System.Collections.Generic;
using Generation.Tiles;
using Graphs.Rooms;
using Settings;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    // Hangs one light in the centre of every room, so each room lights as its own pool.
    public class LightSpawner : PropSpawner
    {
        [SerializeField] private GameObject lightPrefab;

        private void OnEnable() => LightSettings.LightingChanged += SetLights;

        private void OnDisable() => LightSettings.LightingChanged -= SetLights;

        private void SetLights(bool lighting)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(lighting);
        }

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            foreach (var room in graph.rooms)
            {
                if (!rects.TryGetValue(room.id, out var rect)) continue;
                var pos = tilemap.GetCellCenterWorld(new Vector3Int(rect.CenterX, rect.CenterY, 0));
                var go = Instantiate(lightPrefab, pos, Quaternion.identity, transform);
                go.name = $"Light_{room.id}";
                go.SetActive(LightSettings.Lighting);
            }
        }
    }
}