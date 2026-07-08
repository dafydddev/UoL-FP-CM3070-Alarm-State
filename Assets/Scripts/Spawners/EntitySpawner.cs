using System.Collections.Generic;
using Generation.Facility;
using Graphs.Rooms;
using Simulation;

namespace Spawners
{
    // Spawns simulation participants: things that tick, occupy cells, or react to entry.
    // They receive the world context so they can wire their entities into the sim.
    public abstract class EntitySpawner : Spawner
    {
        public abstract void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world);
    }
}
