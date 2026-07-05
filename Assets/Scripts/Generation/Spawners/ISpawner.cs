using System.Collections.Generic;
using Generation.Layout;
using Generation.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Spawners
{
    public interface ISpawner
    {
        public void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap);

        public void ClearChildren();
    }
}