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

        public EntryRules(FacilityGrid grid, OccupancyMap occupancy)
        {
            _grid = grid;
            _occupancy = occupancy;
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

        // Runs the use reaction of any occupant on the cell.
        public void HandleUsed(Vector2Int cell, Actor user)
        {
            var occupant = _occupancy.At(cell);
            if (occupant && occupant.TryGetComponent(out IUseHandler handler)) handler.OnUsed(user);
        }
    }
}
