using System;
using System.Collections.Generic;
using Entities;
using Guards.Actions;
using Guards.Goap;
using Navigation;
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
            new("Chase", 4,
                WorldState.Empty.With(Fact.SeesPlayer, true),
                WorldState.Empty.With(Fact.PlayerCaught, true)),
            new("Investigate", 3,
                WorldState.Empty.With(Fact.HasLead, true),
                WorldState.Empty.With(Fact.HasLead, false)),
            new("RaiseAlarm", 2,
                WorldState.Empty.With(Fact.WantsToRaiseAlarm, true).With(Fact.AlarmRaised, false),
                WorldState.Empty.With(Fact.AlarmRaised, true)),
            PatrolGoal
        };

        // Fires when any guard arrests the player (same idiom as Exit.Reached).
        public static event Action PlayerCaught;

        // Every live guard, so scene-level systems (e.g. the vision field) can iterate them.
        public static readonly List<GuardAgent> Active = new();

        public GridMotor Motor { get; private set; }
        public GuardMemory Memory { get; } = new();

        // Narrow window onto the level's alarm for the guard's actions (Actor.World is protected).
        public AlarmState Alarm => World?.Alarm;

        // And onto the pathfinder, so an action can weigh what a route actually costs before committing to it.
        public AStarPathfinder Pathfinder => World?.Navigator?.Pathfinder;

        // Shown by the debug label so the AI's thinking is visible at a glance.
        public string CurrentGoalName => _goal?.Name ?? "Idle";

        private GoapAction[] _actions;
        private GoapGoal _goal;
        private List<GoapAction> _plan;
        private int _planIndex;

        // A stable per-guard number fixing this guard's slot in the alarm sweep, so responders string out
        // along the escape line (different distances ahead, different sides) instead of bunching on one cell.
        private int _sweepSlot;

        // Called by the spawner after Instantiate, with the patrol route derived from the room graph.
        public void Init(WorldContext world, IReadOnlyList<Vector2Int> patrolRoute)
        {
            base.Init(world);

            var cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            transform.position = world.Navigator.CellToWorld(cell);
            Motor = new GridMotor(this, world, cell);
            _sweepSlot = Mathf.Abs(GetInstanceID());

            _actions = new GoapAction[]
            {
                new PatrolAction(patrolRoute),
                new ChasePlayerAction(),
                new ArrestPlayerAction(),
                new InvestigateLeadAction(),
                new RaiseAlarmAction()
            };
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Active.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Active.Remove(this);
        }

        protected override void Act()
        {
            if (Motor == null) return;

            senses.Sense(World, Motor, Memory);
            BroadcastSighting();
            HearAlarm();
            HearNoise();
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
                .With(Fact.OnPatrol, false)
                .With(Fact.AlarmRaised, World.Alarm.Active)
                .With(Fact.WantsToRaiseAlarm, Memory.WantsToRaiseAlarm);
        }

        // A guard that sees the player while the alarm sounds keeps the contact on their live position and heading,
        // so responders re-sweep where the player actually is and where they are going.
        private void BroadcastSighting()
        {
            if (World.Alarm.Active && Memory.SeesPlayer)
                World.Alarm.UpdateContact(Memory.PlayerCell, Memory.PlayerHeading);
        }

        // While the alarm sounds, a guard within earshot that isn't chasing or on its own trail sweeps the escape line,
        // so responders string out across where the player likely fled.
        // A contact that has moved re-arms it, causing all the guards to scramble again.
        private void HearAlarm()
        {
            var alarm = World.Alarm;
            if (!alarm.Active)
            {
                Memory.ResetAlarmResponse();
                return;
            }

            if (Memory.SeesPlayer || Memory.LeadIsPlayerTrail) return;
            if (Memory.HasAnsweredAlarm(alarm.ContactCell)) return;

            var slot = SweepCell(alarm.ContactCell, alarm.ContactHeading);
            if (IsNear(Motor.Cell, slot))
            {
                Memory.MarkAlarmAnswered(alarm.ContactCell);
                return;
            }

            if (!senses.HearsAlarm(World, Motor)) return;
            Memory.OfferLead(slot, LeadKind.Alarm);
        }

        // This guard's stretch of the escape line: the contact projected a per-guard distance along the heading.
        private Vector2Int SweepCell(Vector2Int contact, Vector2Int heading)
        {
            if (heading == Vector2Int.zero) return contact; // no heading: search the last-seen cell itself

            var along = _sweepSlot % 5 * 2; // 0,2,4,6,8 cells ahead
            var cell = contact;
            for (var i = 0; i < along; i++)
            {
                var next = cell + heading;
                if (!Walkable(next)) break;
                cell = next;
            }

            var lateral = _sweepSlot / 5 % 3 - 1; // one guard on the line, others a step to each side
            var side = new Vector2Int(-heading.y, heading.x) * lateral;
            return Walkable(cell + side) ? cell + side : cell;
        }

        private bool Walkable(Vector2Int cell)
        {
            var tile = World.Grid.At(cell);
            return tile && !tile.BlocksEntry(null);
        }

        // Within one cell (including diagonals) of the target.
        private static bool IsNear(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y)) <= 1;

        // Picks the most important goal that applies and isn't already achieved.
        // Keeps the current plan when the winner hasn't changed; otherwise plans fresh.
        // A goal that can't be planned for falls through to the next,
        // that is the failure recovery: the guard always degrades to something it can do.
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
                    if (!senses.CanSee(World, Motor, cell)) continue;
                    // Paint the ground the guard can watch, not the walls that stop its view.
                    var tile = World.Grid.At(cell);
                    if (!tile || tile.BlocksEntry(null)) continue;
                    into.Add(World.Navigator.CellToWorld(cell));
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

        // A dropped distraction sounds until a guard reaches it and pockets it, so this runs every tick.
        // Whatever is audible right now becomes the lead, and keeps being re-offered until it falls silent.
        // Being the lowest lead kind, it can never displace a player trail or an alarm.
        // A guard with something better to do ignores the noise, then is pulled back to it once that resolves.
        private void HearNoise()
        {
            var noise = DistractionItem.NearestWithin(Motor.Cell, senses.HearingRangeCells);
            if (!noise) return;
            Memory.OfferLead(noise.Cell, LeadKind.Distraction, noise);
        }
    }
}