using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    // Tracks which GameObject occupies each cell.
    public class OccupancyMap
    {
        private readonly Dictionary<Vector2Int, GameObject> _occupants = new();

        public void Place(Vector2Int cell, GameObject occupant) => _occupants[cell] = occupant;
        public void Remove(Vector2Int cell) => _occupants.Remove(cell);
        public GameObject At(Vector2Int cell) => _occupants.GetValueOrDefault(cell);
    }
}
