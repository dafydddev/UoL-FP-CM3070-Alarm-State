using Generation.Facility;
using UnityEngine.Tilemaps;

namespace Simulation
{
    // Everything a spawned entity needs from the level it lives in.
    // Built once per generated level by the orchestrator and injected at spawn time.
    public sealed class WorldContext
    {
        public Tilemap Tilemap { get; }
        public Scheduler Scheduler { get; }
        public SimulationClock Clock { get; }
        public FacilityGrid Grid { get; }
        public OccupancyMap Occupancy { get; }
        public EntryRules Entry { get; }

        public WorldContext(Tilemap tilemap, Scheduler scheduler, SimulationClock clock, FacilityGrid grid)
        {
            Tilemap = tilemap;
            Scheduler = scheduler;
            Clock = clock;
            Grid = grid;
            Occupancy = new OccupancyMap();
            Entry = new EntryRules(grid, Occupancy);
        }
    }
}
