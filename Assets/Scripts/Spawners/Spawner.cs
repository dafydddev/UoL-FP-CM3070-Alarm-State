using System.Collections.Generic;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    public abstract class Spawner : MonoBehaviour
    {
        public abstract void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap);

        public void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }
}