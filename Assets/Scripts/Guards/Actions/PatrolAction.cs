using System.Collections.Generic;
using Guards.Goap;
using UnityEngine;

namespace Guards.Actions
{
    // Walks the guard's patrol route, pausing briefly at each waypoint.
    // Never completes and never fails. An unreachable waypoint is skipped.
    public sealed class PatrolAction : GoapAction
    {
        private const int PauseTicks = 4; // linger at each waypoint before moving on

        private readonly IReadOnlyList<Vector2Int> _route;
        private int _index;
        private int _pause;

        public PatrolAction(IReadOnlyList<Vector2Int> route)
        {
            _route = route;
            Effects = WorldState.Empty.With(Fact.OnPatrol, true);
        }

        // The index survives, so a guard returning from a chase picks the route up where it left off.
        public override void OnEnter(GuardAgent agent)
        {
            _pause = 0;
            if (_route.Count > 0) agent.Motor.SetGoal(_route[_index]);
        }

        public override ActionStatus Run(GuardAgent agent)
        {
            if (_route.Count == 0) return ActionStatus.Running; // no route: hold the post

            if (agent.Motor.HasRoute) return ActionStatus.Running; // still walking

            // Arrived (or the waypoint is unreachable): linger, then head for the next one.
            if (_pause > 0)
            {
                _pause--;
                return ActionStatus.Running;
            }

            _index = (_index + 1) % _route.Count;
            _pause = PauseTicks;
            agent.Motor.SetGoal(_route[_index]); // failure is fine — the next Run advances again
            return ActionStatus.Running;
        }
    }
}