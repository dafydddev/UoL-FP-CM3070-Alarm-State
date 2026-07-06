using System.Collections.Generic;
using Generation.Layout;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Player
{
    // The player as a scheduled actor. The most recently pressed direction owns movement; a press
    // steps one tile and holding repeats after a delay. Moves only onto walkable cells, sliding
    // between cell centres.
    public class PlayerActor : Actor
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField, Min(0.01f)] private float slideDuration = 0.1f;
        [SerializeField, Min(0f)] private float repeatDelay = 0.25f;

        private FacilityGrid _grid;
        private Tilemap _tilemap;
        private Vector2Int _cell;

        private readonly List<Vector2Int> _pressed = new(); // held directions, newest last
        private Vector2Int _owner;
        private Vector2Int _pressPending;
        private float _holdTime;

        private Vector3 _from, _to;
        private float _slide;
        private bool _sliding;

        public void Bind(FacilityGrid grid, Tilemap tilemap)
        {
            _grid = grid;
            _tilemap = tilemap;
            _cell = (Vector2Int)tilemap.WorldToCell(transform.position);
            transform.position = tilemap.GetCellCenterWorld((Vector3Int)_cell);
        }

        private void OnEnable() => moveAction.action.Enable();
        private void OnDisable() => moveAction.action.Disable();

        private void Update()
        {
            RefreshPressed();

            var owner = _pressed.Count > 0 ? _pressed[^1] : Vector2Int.zero;
            if (owner != _owner)
            {
                if (owner != Vector2Int.zero) _pressPending = owner; // newest press takes over
                _owner = owner;
                _holdTime = 0f;
            }
            else
            {
                _holdTime += Time.deltaTime;
            }

            if (_sliding) AdvanceSlide();
        }

        protected override void Act()
        {
            if (_sliding || _grid == null) return;

            Vector2Int dir;
            if (_pressPending != Vector2Int.zero)
            {
                dir = _pressPending;
                _pressPending = Vector2Int.zero;
            }
            else if (_owner != Vector2Int.zero && _holdTime >= repeatDelay)
            {
                dir = _owner;
            }
            else return;

            var target = _cell + dir;
            if (!_grid.IsWalkable(target)) return;

            _cell = target;
            _from = transform.position;
            _to = _tilemap.GetCellCenterWorld((Vector3Int)target);
            _slide = 0f;
            _sliding = true;
        }

        private void AdvanceSlide()
        {
            _slide += Time.deltaTime;
            var t = Mathf.Clamp01(_slide / slideDuration);
            transform.position = Vector3.Lerp(_from, _to, t);
            if (t >= 1f) _sliding = false;
        }

        private void RefreshPressed()
        {
            var v = moveAction.action.ReadValue<Vector2>();
            Track(Vector2Int.right, v.x > 0.5f);
            Track(Vector2Int.left, v.x < -0.5f);
            Track(Vector2Int.up, v.y > 0.5f);
            Track(Vector2Int.down, v.y < -0.5f);
        }

        private void Track(Vector2Int dir, bool held)
        {
            var has = _pressed.Contains(dir);
            if (held && !has) _pressed.Add(dir);
            else if (!held && has) _pressed.Remove(dir);
        }
    }
}