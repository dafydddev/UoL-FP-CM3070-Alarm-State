using Guards.Goap;
using Simulation;
using UnityEngine;

namespace Guards.Actions
{
    // Once its own player-trail has run out,
    // the guard walks to the nearest alarm switch and trips it, broadcasting the player's last-seen cell and heading.
    // Either outcome marks the guard done with the alarm so it won't keep raising it,
    // and it falls back to Patrol when there is no reachable switch; a fresh sighting re-arms it.
    public sealed class RaiseAlarmAction : GoapAction
    {
        private IAlarmSwitch _target;
        private Vector2Int _contactCell, _contactHeading;

        public RaiseAlarmAction()
        {
            Effects = WorldState.Empty.With(Fact.AlarmRaised, true);
        }

        public override void OnEnter(GuardAgent agent)
        {
            _target = agent.Alarm?.NearestSwitch(agent.Motor.Cell);
            _contactCell = agent.Memory.PlayerCell; // where the player was last seen
            _contactHeading = agent.Memory.PlayerHeading; // and which way they were going
            if (_target != null) agent.Motor.SetGoal(_target.Cell);
        }

        public override ActionStatus Run(GuardAgent agent)
        {
            if (agent.Alarm is { Active: true }) // another guard beat this guard to it
            {
                agent.Memory.MarkAlarmSought();
                return ActionStatus.Succeeded;
            }

            if (_target == null || agent.Motor.Blocked)
            {
                agent.Memory.MarkAlarmSought();
                return ActionStatus.Failed;
            }

            if (GuardAgent.IsAdjacent(agent.Motor.Cell, _target.Cell))
            {
                agent.Motor.Stop();
                _target.Activate(_contactCell, _contactHeading);
                agent.Memory.MarkAlarmSought();
                return ActionStatus.Succeeded;
            }

            // Re-route if the walk stalled short of the switch; a dead route means it's unreachable now.
            if (agent.Motor.HasRoute || agent.Motor.SetGoal(_target.Cell)) return ActionStatus.Running;
            agent.Memory.MarkAlarmSought();
            return ActionStatus.Failed;

        }
    }
}