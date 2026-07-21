using System.Collections.Generic;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    public class DistractionItem : MonoBehaviour, IEnterHandler, IInventoryItem
    {
        // Every distraction currently lying dropped in the world.
        // A dropped distraction keeps sounding until something takes it out of the world.
        private static readonly List<DistractionItem> Sounding = new();

        public string distractionId;

        public string ItemId => distractionId;

        // The cell this item currently sits on (meaningful while placed in the world).
        public Vector2Int Cell { get; private set; }

        private WorldContext _world;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            Cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(Cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerInventory inventory)) return;
            inventory.Collect(this);
            _world.Occupancy.Remove(Cell);
            gameObject.SetActive(false);
        }

        // Using a distraction drops it on the user's cell, where it starts sounding.
        // Returns false without placing it if anything else stands there.
        // The player is the only thing a dropped distraction may share a cell with.
        public bool Use(Vector2Int userCell)
        {
            if (!_world.Entry.IsClear(userCell, _world.Player)) return false;
            Cell = userCell;
            transform.position = _world.Tilemap.GetCellCenterWorld((Vector3Int)Cell);
            _world.Occupancy.Place(Cell, gameObject);
            gameObject.SetActive(true);
            if (!Sounding.Contains(this)) Sounding.Add(this);
            return true;
        }

        // The nearest distraction sounding within the given earshot of a cell, or null if none is audible.
        // Chebyshev distance, matching how guards measure earshot everywhere else.
        public static DistractionItem NearestWithin(Vector2Int from, int rangeCells)
        {
            DistractionItem best = null;
            var bestDistance = int.MaxValue;
            foreach (var item in Sounding)
            {
                if (!item) continue; // destroyed between ticks
                var offset = item.Cell - from;
                var distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                if (distance > rangeCells || distance >= bestDistance) continue;
                bestDistance = distance;
                best = item;
            }

            return best;
        }

        // Removes the distraction from the world once a guard has investigated it.
        // Safe to call if the player already picked it back up.
        public void Consume()
        {
            if (!gameObject.activeSelf) return;
            if (_world.Occupancy.At(Cell) == gameObject) _world.Occupancy.Remove(Cell);
            gameObject.SetActive(false);
        }

        // Leaving the world silences the item, whichever way it was removed from the world.
        // For example, consumed by a guard, picked back up by the player, or torn down with the level.
        private void OnDisable() => Sounding.Remove(this);

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(Cell) == gameObject) _world.Occupancy.Remove(Cell);
        }
    }
}