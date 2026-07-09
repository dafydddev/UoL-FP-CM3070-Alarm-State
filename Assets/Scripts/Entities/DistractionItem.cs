using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    public class DistractionItem : MonoBehaviour, IEnterHandler
    {
        public string distractionId;

        private Vector2Int _cell;
        private WorldContext _world;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
        }

        public void OnEntered(Actor mover)
        {
            if (!mover.TryGetComponent(out PlayerInventory inventory)) return;
            inventory.CollectDistraction(this);
            _world.Occupancy.Remove(_cell);
            gameObject.SetActive(false);
        }
        
        // Places the distraction on the cell nearest the given world position
        public void Drop(Vector3 worldPos)
        {
            _cell = (Vector2Int)_world.Tilemap.WorldToCell(worldPos);
            transform.position = _world.Tilemap.GetCellCenterWorld((Vector3Int)_cell);
            _world.Occupancy.Place(_cell, gameObject);
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}



// using System.Collections.Generic;
// using UnityEngine;
//
// namespace Entities
// {
//     // A throwable distraction.
//     // Before being thrown, it can be picked up by the player.
//     // Once dropped, it sits in the world as a point of interest for guards to investigate.
//     public class DistractionItem : MonoBehaviour
//     {
//         // True once thrown/dropped. Pickup is only allowed before this is set.
//         public bool Dropped { get; private set; }
//
//         // All dropped distractions currently in the world, for Nearest() lookups.
//         private static readonly List<DistractionItem> Active = new();
//
//         // Re-register with the active list when re-enabled, but only if already dropped.
//         private void OnEnable()
//         {
//             if (Dropped)
//             {
//                 Active.Add(this);
//             }
//         }
//
//         // Drop out of the active list whenever disabled (e.g. while being carried).
//         private void OnDisable() => Active.Remove(this);
//
//         // Throws/places the distraction at the given position and makes it active in the world.
//         public void Drop(Vector3 pos)
//         {
//             transform.position = pos;
//             Dropped = true;
//             gameObject.SetActive(true);
//             if (!Active.Contains(this))
//             {
//                 // guard against double-adds
//                 Active.Add(this);
//             }
//         }
//
//         // Returns the closest dropped distraction to the given point, or null if there are none.
//         public static DistractionItem Nearest(Vector3 from)
//         {
//             DistractionItem best = null;
//             var bestSqr = float.MaxValue;
//             foreach (var d in Active)
//             {
//                 // Skip destroyed or not-yet-dropped entries.
//                 if (!d || !d.Dropped) continue;
//                 // Compare squared distances to avoid a square root per candidate.
//                 var sqr = (d.transform.position - from).sqrMagnitude;
//                 if (!(sqr < bestSqr)) continue;
//                 bestSqr = sqr;
//                 best = d;
//             }
//
//             return best;
//         }
//
//         // Clears the active list, e.g. when regenerating/resetting the level.
//         public static void Clear() => Active.Clear();
//     }
// }