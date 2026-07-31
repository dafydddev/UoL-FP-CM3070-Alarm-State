using System.Collections.Generic;
using System.Linq;
using Effects;
using UnityEngine;

namespace Guards
{
    // Paints the cells guards can currently see straight into the game view (and builds), as one shared surface.
    // Cones of guards that can see the player are painted in the alert colour, the rest in the calm colour.
    public class GuardVisionField : MonoBehaviour
    {
        [SerializeField] private bool show = true;
        [SerializeField] private Color calmColor = new(1f, 0.95f, 0.4f, 0.16f); // patrolling
        [SerializeField] private Color alertColor = new(1f, 0.25f, 0.2f, 0.28f); // player in sight
        [SerializeField, Range(0.1f, 1f)] private float cellFill = 0.9f; // marker size vs the tile
        [SerializeField] private int sortingOrder = 1; // over the floor, under the guard sprites
        [SerializeField, Min(0.1f)] private float refreshFallbackSeconds = 0.5f; // staleness bound for cached cones

        // A cached cone: the cells a guard saw, and where it stood and faced when they were collected.
        private sealed class Cone
        {
            public readonly List<Vector3> Cells = new();
            public Vector2Int Cell;
            public Vector2Int Facing;
            public float RefreshedAt = float.NegativeInfinity;
        }

        private readonly Dictionary<GuardAgent, Cone> _cones = new();
        private readonly List<GuardAgent> _dead = new();
        private readonly HashSet<Vector3> _calmCells = new();
        private readonly HashSet<Vector3> _alertCells = new();

        private CellSurface _calm;
        private CellSurface _alert;
        private Vector2 _cellSize = Vector2.one;

        private void Awake()
        {
            _calm = new CellSurface(transform, "CalmVision", sortingOrder);
            _alert = new CellSurface(transform, "AlertVision", sortingOrder);
        }

        private void LateUpdate()
        {
            _calm.SetVisible(show);
            _alert.SetVisible(show);
            if (!show) return;

            _calmCells.Clear();
            _alertCells.Clear();

            foreach (var guard in GuardAgent.Active)
            {
                var cone = ConeFor(guard);
                Refresh(guard, cone);
                var target = guard.Memory.SeesPlayer ? _alertCells : _calmCells;
                foreach (var cell in cone.Cells) target.Add(cell);
            }

            _calmCells.ExceptWith(_alertCells); // a cell inside an alert cone reads alert, painted once

            var half = _cellSize * (0.5f * cellFill);
            _calm.Rebuild(_calmCells, half, calmColor);
            _alert.Rebuild(_alertCells, half, alertColor);

            PruneDead();
        }

        private Cone ConeFor(GuardAgent guard)
        {
            if (_cones.TryGetValue(guard, out var cone)) return cone;
            cone = new Cone();
            _cones.Add(guard, cone);
            return cone;
        }

        // Recollects a guard's visible cells only when it has moved or turned since the last collection,
        private void Refresh(GuardAgent guard, Cone cone)
        {
            var motor = guard.Motor;
            if (motor == null) return;
            if (cone.Cell == motor.Cell && cone.Facing == motor.Facing &&
                Time.time - cone.RefreshedAt < refreshFallbackSeconds) return;
            guard.CollectVisibleCells(cone.Cells, out _cellSize);
            cone.Cell = motor.Cell;
            cone.Facing = motor.Facing;
            cone.RefreshedAt = Time.time;
        }

        // Drops cache entries for guards that no longer exist (level rebuilds destroy and respawn them).
        private void PruneDead()
        {
            if (_cones.Count == GuardAgent.Active.Count) return;
            foreach (var pair in _cones.Where(pair => !pair.Key))
            {
                _dead.Add(pair.Key);
            }

            foreach (var guard in _dead) _cones.Remove(guard);
            _dead.Clear();
        }

        private void OnDestroy()
        {
            _calm?.Dispose();
            _alert?.Dispose();
        }
    }
}