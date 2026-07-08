using System.Collections.Generic;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerActor : Actor
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField, Min(0f)] private float repeatDelay = 0.2f;

        private Vector2Int _cell, _prevCell;
        private readonly List<Vector2Int> _pressed = new();
        private Vector2Int _owner, _pending;
        private float _holdTime;

        protected override void OnEnable()
        {
            base.OnEnable();
            moveAction.action.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            moveAction.action.Disable();
        }

        public override void Init(WorldContext world)
        {
            base.Init(world);
            var tilemap = world.Tilemap;
            _cell = _prevCell = (Vector2Int)tilemap.WorldToCell(transform.position);
            transform.position = tilemap.GetCellCenterWorld((Vector3Int)_cell);
        }

        private void Update()
        {
            if (World == null) return;
            ReadInput();
            var tilemap = World.Tilemap;
            transform.position = Vector3.Lerp(
                tilemap.GetCellCenterWorld((Vector3Int)_prevCell),
                tilemap.GetCellCenterWorld((Vector3Int)_cell),
                Mathf.Clamp01(World.Clock.Alpha));
        }

        protected override void Act()
        {
            _prevCell = _cell;

            Vector2Int dir;
            if (_pending != Vector2Int.zero) { dir = _pending; _pending = Vector2Int.zero; }
            else if (_owner != Vector2Int.zero && _holdTime >= repeatDelay) dir = _owner;
            else return;

            var target = _cell + dir;
            if (!World.Entry.CanEnter(target, this)) return;

            _cell = target;
            World.Entry.HandleEntered(target, this);
        }

        private void ReadInput()
        {
            var v = moveAction.action.ReadValue<Vector2>();
            Track(Vector2Int.right, v.x > 0.5f);
            Track(Vector2Int.left, v.x < -0.5f);
            Track(Vector2Int.up, v.y > 0.5f);
            Track(Vector2Int.down, v.y < -0.5f);

            var owner = _pressed.Count > 0 ? _pressed[^1] : Vector2Int.zero;
            if (owner != _owner)
            {
                if (owner != Vector2Int.zero) _pending = owner;
                _owner = owner;
                _holdTime = 0f;
            }
            else _holdTime += Time.deltaTime;
        }

        private void Track(Vector2Int dir, bool held)
        {
            var has = _pressed.Contains(dir);
            if (held && !has) _pressed.Add(dir);
            else if (!held && has) _pressed.Remove(dir);
        }
    }
}