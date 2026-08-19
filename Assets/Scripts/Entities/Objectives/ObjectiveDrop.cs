using Simulation;
using UnityEngine;

namespace Entities.Objectives
{
    // The reward a secondary objective leaves behind when complete.
    public class ObjectiveDrop : MonoBehaviour
    {
        // Where a drop can land, searched in this order for the first cell that is free.
        private static readonly Vector2Int[] Around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // The items that can appear. Each as likely as the others once a drop is rolled.
        [SerializeField] private GameObject[] rewardPrefabs;

        // The chance that completing the objective rewards anything at all.
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.5f;

        // Rolls for a reward and places it beside the objective.
        // Does nothing if the roll fails, there is nothing to drop, or no cell alongside is free.
        public void Roll(WorldContext world, int seed)
        {
            if (rewardPrefabs == null || rewardPrefabs.Length == 0) return;

            var rng = new System.Random(seed);
            if (rng.NextDouble() >= dropChance) return;

            var prefab = rewardPrefabs[rng.Next(rewardPrefabs.Length)];
            if (!prefab) return;

            var from = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            foreach (var direction in Around)
            {
                var cell = from + direction;

                // The cell has to be walkable and empty: an occupied one would be overwritten on the map.
                if (world.Occupancy.At(cell) || !world.Entry.CanEnter(cell, null)) continue;

                // Parented alongside the objective, so the drop is torn down with the rest of the level.
                var go = Instantiate(prefab, world.Tilemap.GetCellCenterWorld((Vector3Int)cell),
                    Quaternion.identity, transform.parent);
                go.GetComponent<ISpawnedEntity>()?.Init(world);
                return;
            }
        }
    }
}
