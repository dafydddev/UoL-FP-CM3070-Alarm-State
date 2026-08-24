using System.Linq;
using Generation.Cells;
using Generation.Facility;
using UnityEngine;

namespace Simulation
{
    // Decides whether a cell can be entered and runs entry reactions.
    public class EntryRules
    {
        private readonly FacilityGrid _grid;
        private readonly OccupancyMap _occupancy;
        private readonly Scheduler _scheduler;

        public EntryRules(FacilityGrid grid, OccupancyMap occupancy, Scheduler scheduler)
        {
            _grid = grid;
            _occupancy = occupancy;
            _scheduler = scheduler;
        }

        // Whether nothing stands on the cell: no placed entity, and no actor bar the one excused.
        // Actors are read from the scheduler rather than the occupancy map, which holds placed entities.
        public bool IsClear(Vector2Int cell, Actor excusing = null)
        {
            if (_occupancy.At(cell)) return false;
            return _scheduler.Actors.All(actor => !actor || actor == excusing || actor.Cell != cell);
        }

        // The terrain must exist and not block, and no occupant may block either.
        public bool CanEnter(Vector2Int cell, Actor mover)
        {
            var tile = _grid.At(cell);
            if (!tile || tile.BlocksEntry(mover)) return false;
            var occupant = _occupancy.At(cell);
            return !(occupant && occupant.TryGetComponent(out IEntryBlocker blocker) && blocker.BlocksEntry(mover));
        }

        // Runs the entry reactions of both the terrain and any occupant.
        public void HandleEntered(Vector2Int cell, Actor mover)
        {
            _grid.At(cell)?.OnEntered(mover);

            var occupant = _occupancy.At(cell);
            if (occupant && occupant.TryGetComponent(out IEnterHandler handler)) handler.OnEntered(mover);
        }
        
        public void HandleExited(Vector2Int cell, Actor mover)
        {
            var occupant = _occupancy.At(cell);
            if (occupant && occupant.TryGetComponent(out IExitHandler handler)) handler.OnExited(mover);
        }

        // Whether an occupant on the cell would react to being used, without using it.
        public bool CanUse(Vector2Int cell, Actor user)
        {
            var occupant = _occupancy.At(cell);
            return occupant && occupant.TryGetComponent(out IUseHandler handler) && handler.CanUse(user);
        }

        // Runs the use reaction of any occupant on the cell, reporting whether one activated.
        public bool HandleUsed(Vector2Int cell, Actor user)
        {
            var occupant = _occupancy.At(cell);
            return occupant && occupant.TryGetComponent(out IUseHandler handler) && handler.OnUsed(user);
        }
    }
}
