using System;
using System.Collections.Generic;
using Entities;
using Guards.Actions;
using Guards.Goap;
using Simulation;
using UnityEngine;

namespace Guards
{
    // The guard's brain, driven by the simulation tick like every other actor.
    // Each Act():
    // - sense the world, snapshot it as facts, pick the most important relevant goal,
    // - plan a route of actions to it (replanning only when the goal changes or an action fails),
    // - run the current action, then take one grid step.
    // Interruption is central: a higher-priority goal becoming relevant simply wins the next selection,
    // so actions never need to know about the goal hierarchy.
    public class GuardAgent : Actor
    {
        [SerializeField] private GuardSenses senses = new();

        // How often the guard takes a grid step (in ticks; 1 matches the player's pace).
        [SerializeField, Min(1)] private int patrolStepEveryTicks = 3; // while calm
        [SerializeField, Min(1)] private int alertStepEveryTicks = 2; // while chasing or investigating

        private static readonly GoapGoal PatrolGoal = new("Patrol", 1,
            WorldState.Empty,
            WorldState.Empty.With(Fact.OnPatrol, true));

        // Goals in one shared, immutable table: what matters more, when it applies, what it wants.
        private static readonly GoapGoal[] Goals =
        {
            new("Chase", 3,
                WorldState.Empty.With(Fact.SeesPlayer, true),
                WorldState.Empty.With(Fact.PlayerCaught, true)),
            new("Investigate", 2,
                WorldState.Empty.With(Fact.HasLead, true),
                WorldState.Empty.With(Fact.HasLead, false)),
            PatrolGoal
        };

        // Fires when any guard arrests the player (same idiom as Exit.Reached).
        public static event Action PlayerCaught;

        public GridMotor Motor { get; private set; }
        public GuardMemory Memory { get; } = new();

        // Shown by the debug label so the AI's thinking is visible at a glance.
        public string CurrentGoalName => _goal?.Name ?? "Idle";

        private GoapAction[] _actions;
        private GoapGoal _goal;
        private List<GoapAction> _plan;
        private int _planIndex;

        // Called by the spawner after Instantiate, with the patrol route derived from the room graph.
        public void Init(WorldContext world, IReadOnlyList<Vector2Int> patrolRoute)
        {
            base.Init(world);

            var cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            transform.position = world.Navigator.CellToWorld(cell);
            Motor = new GridMotor(this, world, cell);

            _actions = new GoapAction[]
            {
                new PatrolAction(patrolRoute),
                new ChasePlayerAction(),
                new ArrestPlayerAction(),
                new InvestigateLeadAction()
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            DistractionItem.Dropped += OnDistractionDropped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DistractionItem.Dropped -= OnDistractionDropped;
        }

        protected override void Act()
        {
            if (Motor == null) return;

            senses.Sense(World, Motor, Memory);

            Think(Snapshot());

            // Calm guards amble; anything more urgent quickens the step.
            Motor.StepEveryTicks = _goal == null || _goal == PatrolGoal ? patrolStepEveryTicks : alertStepEveryTicks;

            RunCurrentAction();
            Motor.Step();
        }

        private void Update()
        {
            if (World == null || Motor == null) return;
            if (GameLock.Locked) return;
            transform.position = Motor.RenderPosition(World.Clock.Alpha);
        }

        // The facts as they stand this tick.
        // Every fact is specified, so goals and preconditions can also test for absence (e.g. HasLead == false).
        private WorldState Snapshot()
        {
            var atPlayer = Memory.SeesPlayer && IsAdjacent(Motor.Cell, Memory.PlayerCell);
            return WorldState.Empty
                .With(Fact.SeesPlayer, Memory.SeesPlayer)
                .With(Fact.AtPlayer, atPlayer)
                .With(Fact.PlayerCaught, false)
                .With(Fact.HasLead, Memory.HasLead)
                .With(Fact.OnPatrol, false);
        }

        // Picks the most important goal that applies and isn't already achieved.
        // Keeps the current plan when the winner hasn't changed; otherwise plans fresh.
        // A goal that can't be planned for falls through to the next — that is the failure recovery:
        // the guard always degrades to something it can do.
        private void Think(WorldState snapshot)
        {
            GoapGoal best = null;
            foreach (var goal in Goals)
            {
                if (best != null && goal.Priority <= best.Priority) continue;
                if (!snapshot.Satisfies(goal.RelevantWhen)) continue;
                if (snapshot.Satisfies(goal.Desired)) continue;
                best = goal;
            }

            while (best != null)
            {
                if (best == _goal && _plan != null) return; // already pursuing it

                var plan = GoapPlanner.Plan(snapshot, best.Desired, _actions);
                if (plan is { Count: > 0 })
                {
                    SwitchTo(best, plan);
                    return;
                }

                best = NextGoalBelow(best.Priority, snapshot);
            }

            AbandonPlan(); // nothing worth doing or nothing plannable; stand watch
        }

        private static GoapGoal NextGoalBelow(int priority, WorldState snapshot)
        {
            GoapGoal best = null;
            foreach (var goal in Goals)
            {
                if (goal.Priority >= priority) continue;
                if (best != null && goal.Priority <= best.Priority) continue;
                if (!snapshot.Satisfies(goal.RelevantWhen)) continue;
                if (snapshot.Satisfies(goal.Desired)) continue;
                best = goal;
            }

            return best;
        }

        private void SwitchTo(GoapGoal goal, List<GoapAction> plan)
        {
            Motor.Stop();
            _goal = goal;
            _plan = plan;
            _planIndex = 0;
            _plan[0].OnEnter(this);
        }

        private void AbandonPlan()
        {
            if (_plan == null && _goal == null) return;
            Motor.Stop();
            _goal = null;
            _plan = null;
        }

        private void RunCurrentAction()
        {
            if (_plan == null) return;

            switch (_plan[_planIndex].Run(this))
            {
                case ActionStatus.Running:
                    break;
                case ActionStatus.Succeeded:
                    if (++_planIndex >= _plan.Count) AbandonPlan(); // goal reached; reselect next tick
                    else _plan[_planIndex].OnEnter(this);
                    break;
                case ActionStatus.Failed:
                    AbandonPlan(); // next tick's Think replans or falls back
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Every cell this guard can currently see (range + cone + line of sight).
        // Reports the world size of one grid cell so an overlay can size its markers to the tiles.
        // Yields nothing until the guard has been initialised.
        public void CollectVisibleCells(List<Vector3> into, out Vector2 cellWorldSize)
        {
            into.Clear();
            cellWorldSize = Vector2.one;
            if (Motor == null) return;
            var origin = World.Navigator.CellToWorld(Motor.Cell);
            cellWorldSize = new Vector2(
                World.Navigator.CellToWorld(Motor.Cell + Vector2Int.right).x - origin.x,
                World.Navigator.CellToWorld(Motor.Cell + Vector2Int.up).y - origin.y);

            var range = senses.ViewRangeCells;
            for (var dx = -range; dx <= range; dx++)
            {
                for (var dy = -range; dy <= range; dy++)
                {
                    var cell = Motor.Cell + new Vector2Int(dx, dy);
                    if (senses.CanSee(World, Motor, cell)) into.Add(World.Navigator.CellToWorld(cell));
                }
            }
        }

        // Raised by the arrest action once it has held the player long enough.
        public static void RaisePlayerCaught()
        {
            PlayerCaught?.Invoke();
        }

        // Adjacency on the 4-connected grid (or sharing a cell) counts as within reach.
        public static bool IsAdjacent(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= 1;

        // A dropped distraction is a noise: any guard within earshot remembers it as a lead.
        private void OnDistractionDropped(DistractionItem item)
        {
            if (Motor == null) return;
            var offset = item.Cell - Motor.Cell;
            var distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            if (distance > senses.HearingRangeCells) return;
            Memory.OfferLead(item.Cell, LeadKind.Distraction, item);
        }
    }
}