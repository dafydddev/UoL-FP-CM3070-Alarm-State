using System.Collections.Generic;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using UnityEngine.Tilemaps;

namespace Spawners
{
    // Spawns set dressing: placed once, never consulted by the simulation.
    public abstract class PropSpawner : Spawner
    {
        public abstract void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap);
    }
}
