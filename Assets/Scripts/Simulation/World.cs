using System.Collections.Generic;
using Generation.Cells;
using Generation.Facility;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Simulation
{
    // The one shared world every actor reads from: the sim, the current level, and what occupies it.
    public class World : MonoBehaviour
    {
        public static World Instance { get; private set; }

        [SerializeField] private Scheduler scheduler;
        [SerializeField] private SimulationClock clock;
        [SerializeField] private Tilemap tilemap;

        public Scheduler Scheduler => scheduler;
        public SimulationClock Clock => clock;
        public Tilemap Tilemap => tilemap;
        public FacilityGrid Grid { get; set; }

        private readonly Dictionary<Vector2Int, GameObject> _occupants = new();

        public void Place(Vector2Int cell, GameObject occupant) => _occupants[cell] = occupant;
        public void Remove(Vector2Int cell) => _occupants.Remove(cell);
        public GameObject OccupantAt(Vector2Int cell) => _occupants.GetValueOrDefault(cell);

        // The terrain must exist and not block, and no occupant may block either.
        public bool CanEnter(Vector2Int cell, Actor mover)
        {
            var tile = Grid?.At(cell);
            if (!tile || tile.BlocksEntry(mover)) return false;

            var occupant = OccupantAt(cell);
            return !(occupant && occupant.TryGetComponent(out IEntryBlocker blocker) && blocker.BlocksEntry(mover));
        }

        // Runs the entry reactions of both the terrain and any occupant.
        public void HandleEntered(Vector2Int cell, Actor mover)
        {
            Grid?.At(cell)?.OnEntered(mover);

            var occupant = OccupantAt(cell);
            if (occupant && occupant.TryGetComponent(out IEnterHandler handler)) handler.OnEntered(mover);
        }

        private void Awake() => Instance = this;
    }
}