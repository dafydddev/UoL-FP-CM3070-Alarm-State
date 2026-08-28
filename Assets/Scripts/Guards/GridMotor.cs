using System.Collections.Generic;
using Simulation;
using UnityEngine;

namespace Guards
{
    // Moves an actor across the grid one cell per tick, following A* routes from the world's navigator.
    // Owns the actor's logical cell. Mirrors the player's movement idiom, entering cells through EntryRules.
    public class GridMotor
    {
        // After a pathfind fails, don't retry the same goal for this many ticks.
        // Makes sure that an unreachable target can't trigger a pointless A* search every tick.
        private const int PathRetryTicks = 10;

        // Ceiling for the ticks-since-move counter so idling can't overflow it.
        private const int TickCap = 1000;

        private readonly Actor _owner;
        private readonly WorldContext _world;
        private readonly Queue<Vector2Int> _route = new();
        private Vector2Int _goal; // where the current route leads, for replanning mid-route

        private Vector2Int _failedGoal;
        private int _failedMemoTicks;
        private int _ticksSinceMove = TickCap; // large so the first order moves immediately

        public Vector2Int Cell { get; private set; }
        private Vector2Int PrevCell { get; set; }
        public Vector2Int Facing { get; private set; } = Vector2Int.right; // direction of the last step

        // Move one cell per this many ticks, e.g. 1 = every tick, which is the player's pace.
        public int StepEveryTicks { get; set; } = 1;

        public bool HasRoute => _route.Count > 0;

        // Set when the route turned out impossible. Cleared by the next SetGoal or Stop.
        public bool Blocked { get; private set; }

        public GridMotor(Actor owner, WorldContext world, Vector2Int startCell)
        {
            _owner = owner;
            _world = world;
            Cell = PrevCell = startCell;
        }

        // Routes to the goal cell. Returns false if no path exists for this actor right now.
        // A goal already stood on clears the route and reports success, so callers need not test for it.
        public bool SetGoal(Vector2Int goal)
        {
            Blocked = false;
            if (goal == Cell)
            {
                _route.Clear();
                return true;
            }

            if (_failedMemoTicks > 0 && goal == _failedGoal) return false;

            var cells = _world.Navigator.Pathfinder.FindPath(Cell, goal, _owner);
            if (cells == null)
            {
                _failedGoal = goal;
                _failedMemoTicks = PathRetryTicks;
                _route.Clear();
                return false;
            }

            _route.Clear();
            for (var i = 1; i < cells.Count; i++) _route.Enqueue(cells[i]); // cells[0] is where we stand
            _goal = goal;
            return true;
        }

        public void Stop()
        {
            _route.Clear();
            Blocked = false;
        }
        
        public void Step()
        {
            if (_failedMemoTicks > 0) _failedMemoTicks--;
            if (_ticksSinceMove < TickCap) _ticksSinceMove++;
            if (_route.Count == 0) return;
            if (_ticksSinceMove < StepEveryTicks) return; // not this guard's tick to move

            var next = _route.Peek();
            if (_world.Entry.CanEnter(next, _owner))
            {
                _route.Dequeue();
                PrevCell = Cell;
                Facing = next - Cell;
                Cell = next;
                _ticksSinceMove = 0;
                _world.Entry.HandleEntered(next, _owner);
                _world.Entry.HandleExited(PrevCell, _owner);
                return;
            }

            // Something now blocks the step — route around it, or report the way shut.
            if (!SetGoal(_goal)) Blocked = true;
        }

        // Where to draw the guard this frame, allowing guards to transition between cells.
        // Clamped, so a guard standing still is drawn on the cell it holds rather than past it.
        public Vector3 RenderPosition(float alpha)
        {
            var progress = _ticksSinceMove + alpha;
            return Vector3.Lerp(
                _world.Navigator.CellToWorld(PrevCell),
                _world.Navigator.CellToWorld(Cell),
                Mathf.Clamp01(progress));
        }
    }
}