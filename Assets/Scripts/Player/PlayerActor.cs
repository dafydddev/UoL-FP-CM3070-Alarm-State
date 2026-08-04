using System.Collections.Generic;
using Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerActor : Actor
    {
        [Header("Input Actions")] [SerializeField]
        private InputActionReference upAction;

        [SerializeField] private InputActionReference downAction;
        [SerializeField] private InputActionReference leftAction;
        [SerializeField] private InputActionReference rightAction;
        [SerializeField] private InputActionReference moveToAction;
        [SerializeField] private InputActionReference pointAction;
        [SerializeField] private InputActionReference useAction;

        [Header("Movement Settings")] [SerializeField, Min(0f)]
        private float repeatDelay = 0.2f;

        [SerializeField] private LineRenderer routePreview;
        [SerializeField] private SpriteRenderer routeMarker;

        private Vector2Int _cell, _prevCell;
        private readonly List<Vector2Int> _pressed = new();
        private Vector2Int _owner, _pending;
        private float _holdTime;

        // Route queued by a click, consumed one cell per tick so guards keep pace.
        private readonly Queue<Vector2Int> _route = new();
        private Vector2Int _routeGoal;
        private UnityEngine.Camera _camera;

        private PlayerInventory _inventory;

        public override Vector2Int Cell => _cell;

        // The cell the use key acts on: the one being drawn under the player, which part-way
        // through a step is still the cell being left rather than the one being moved into.
        private Vector2Int UseCell => (Vector2Int)World.Tilemap.WorldToCell(transform.position);

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
            _inventory = GetComponent<PlayerInventory>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            upAction.action.Enable();
            downAction.action.Enable();
            leftAction.action.Enable();
            rightAction.action.Enable();
            moveToAction.action.Enable();
            pointAction.action.Enable();
            useAction.action.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            upAction.action.Disable();
            downAction.action.Disable();
            leftAction.action.Disable();
            rightAction.action.Disable();
            moveToAction.action.Disable();
            pointAction.action.Disable();
            useAction.action.Disable();
        }

        public override void Init(WorldContext world)
        {
            var tilemap = world.Tilemap;
            _cell = _prevCell = (Vector2Int)tilemap.WorldToCell(transform.position);
            transform.position = tilemap.GetCellCenterWorld((Vector3Int)_cell);

            base.Init(world); // after the cell, so Cell reads true from the moment the scheduler holds us
        }

        private void Update()
        {
            if (World == null) return;
            if (GameLock.Locked) return;
            if (InputCapture.Captured) return;
            ReadInput();
            ReadClick();
            ReadUse();
            var tilemap = World.Tilemap;
            transform.position = Vector3.Lerp(
                tilemap.GetCellCenterWorld((Vector3Int)_prevCell),
                tilemap.GetCellCenterWorld((Vector3Int)_cell),
                Mathf.Clamp01(World.Clock.Alpha));
            DrawRoutePreview();
            DrawRouteMarker();
        }

        protected override void Act()
        {
            _prevCell = _cell;

            // Ticks keep coming while a minigame has the keys; stand still rather than walk a queued route.
            if (InputCapture.Captured) return;

            Vector2Int dir;
            if (_pending != Vector2Int.zero)
            {
                dir = _pending;
                _pending = Vector2Int.zero;
            }
            else if (_owner != Vector2Int.zero && _holdTime >= repeatDelay) dir = _owner;
            else
            {
                FollowRoute();
                return;
            }

            _route.Clear(); // direct input overrides a queued click order

            var target = _cell + dir;
            if (!World.Entry.CanEnter(target, this)) return;

            _cell = target;
            World.Entry.HandleEntered(target, this);
            World.Entry.HandleExited(_prevCell, this);
        }

        // Takes the next step of a queued route, replanning if the world changed underneath it.
        private void FollowRoute()
        {
            if (_route.Count == 0) return;

            var next = _route.Peek();
            if (World.Entry.CanEnter(next, this))
            {
                _route.Dequeue();
                _cell = next;
                World.Entry.HandleEntered(next, this);
                World.Entry.HandleExited(_prevCell, this);
            }
            else PlanRoute(_routeGoal); // something now blocks the step — route around it or stop
        }

        private void ReadInput()
        {
            Track(Vector2Int.up, upAction.action.IsPressed());
            Track(Vector2Int.down, downAction.action.IsPressed());
            Track(Vector2Int.left, leftAction.action.IsPressed());
            Track(Vector2Int.right, rightAction.action.IsPressed());

            var owner = _pressed.Count > 0 ? _pressed[^1] : Vector2Int.zero;
            if (owner != _owner)
            {
                if (owner != Vector2Int.zero) _pending = owner;
                _owner = owner;
                _holdTime = 0f;
            }
            else _holdTime += Time.deltaTime;
        }

        // Turns a click on the grid into a queued route.
        // Clicks on UI, walls, or unreachable cells are ignored.
        private void ReadClick()
        {
            if (!moveToAction.action.WasPressedThisFrame()) return;
            if (!_camera) return;
            var eventSystem = EventSystem.current;
            if (eventSystem && eventSystem.IsPointerOverGameObject()) return;

            var world = _camera.ScreenToWorldPoint(pointAction.action.ReadValue<Vector2>());
            PlanRoute((Vector2Int)World.Tilemap.WorldToCell(world));
        }

        // Presses the use key to interact with whatever the player is standing on.
        // The cell activates first, so an alarm or an unhacked objective takes the key rather than an item.
        private void ReadUse()
        {
            if (!useAction.action.WasPressedThisFrame()) return;
            var cell = UseCell;
            if (World.Entry.HandleUsed(cell, this)) return;
            if (_inventory) _inventory.TryUse(cell);
        }

        private void PlanRoute(Vector2Int goal)
        {
            _route.Clear();
            var cells = World.Navigator.Pathfinder.FindPath(_cell, goal, this);
            if (cells == null) return;

            _routeGoal = goal;
            for (var i = 1; i < cells.Count; i++) _route.Enqueue(cells[i]); // cells[0] is where we stand
        }

        // Redraws the preview line from the player through the remaining route cells.
        private void DrawRoutePreview()
        {
            if (!routePreview) return;

            routePreview.positionCount = _route.Count == 0 ? 0 : _route.Count + 2;
            if (_route.Count == 0) return;

            routePreview.SetPosition(0, transform.position);
            routePreview.SetPosition(1, World.Tilemap.GetCellCenterWorld((Vector3Int)_cell));
            var i = 2;
            foreach (var cell in _route)
            {
                routePreview.SetPosition(i++, World.Tilemap.GetCellCenterWorld((Vector3Int)cell));
            }
        }
        
        // Sits the marker on the clicked cell while the route lasts.
        private void DrawRouteMarker()
        {
            if (!routeMarker) return;

            routeMarker.enabled = _route.Count > 0;
            if (_route.Count == 0) return;

            routeMarker.transform.position = World.Tilemap.GetCellCenterWorld((Vector3Int)_routeGoal);
        }

        private void Track(Vector2Int dir, bool held)
        {
            var has = _pressed.Contains(dir);
            if (held && !has) _pressed.Add(dir);
            else if (!held && has) _pressed.Remove(dir);
        }
    }
}