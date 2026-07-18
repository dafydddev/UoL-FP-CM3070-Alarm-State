using System.Collections.Generic;
using Simulation;
using UnityEngine;

namespace Guards
{
    // Moves an actor across the grid one cell per tick, following A* routes from the world's navigator.
    // Owns the actor's logical cell (the transform only renders it).
    // Mirrors the player's movement idiom: enter cells through EntryRules so doors,
    // occupants and future rules apply to guards exactly as they do to the player.
    public class GridMotor
    {
        // After a pathfind fails, don't retry the same goal for this many ticks,
        // so an unreachable target can't trigger an A* search every tick.
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

        // Move one cell per this many ticks (1 = every tick, i.e. the player's pace).
        // The agent sets it each tick from its patrol/alert tuning.
        public int StepEveryTicks { get; set; } = 1;

        public bool HasRoute => _route.Count > 0;

        // Set when the route (or a replan around an obstacle) turned out impossible;
        // cleared by the next SetGoal or Stop.
        public bool Blocked { get; private set; }

        public GridMotor(Actor owner, WorldContext world, Vector2Int startCell)
        {
            _owner = owner;
            _world = world;
            Cell = PrevCell = startCell;
        }

        // Routes to the goal cell. Returns false (and remembers the failure briefly)
        // if no path exists for this actor right now.
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

        // Advances at most one cell, and only on every StepEveryTicks-th tick —
        // that is what makes a guard slower than the player. 
        // Called exactly once per agent tick, after thinking.
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

        // Where to draw the guard this frame: the hop to the current cell plays out over the single tick it happened in —
        // the same stepped look as the player — and the guard then rests on the cell until its next move tick.
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
