using System.Collections.Generic;
using Generation.Layout;
using Generation.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Spawners
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