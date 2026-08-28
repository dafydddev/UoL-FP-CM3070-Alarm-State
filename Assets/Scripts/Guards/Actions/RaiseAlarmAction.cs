using Guards.Goap;
using Simulation;
using UnityEngine;

namespace Guards.Actions
{
    // Once its own player-trail has run out, the guard walks to the nearest alarm switch and trips it.
    // This broadcasts the player's last-seen cell and heading.
    // Either outcome marks the guard done with the alarm so it won't keep raising it.
    // Falls back to Patrol when there is no reachable switch. A fresh sighting of the player re-arms the guard.
    public sealed class RaiseAlarmAction : GoapAction
    {
        private IAlarmSwitch _target;
        private Vector2Int _contactCell, _contactHeading;

        public RaiseAlarmAction()
        {
            Effects = WorldState.Empty.With(Fact.AlarmRaised, true);
        }

        // The contact is taken once, at the outset, so the broadcast reports the sighting that sent the guard.
        public override void OnEnter(GuardAgent agent)
        {
            _contactCell = agent.Memory.PlayerCell; // where the player was last seen
            _contactHeading = agent.Memory.PlayerHeading; // and which way they were going
            Target(agent);
        }

        public override ActionStatus Run(GuardAgent agent)
        {
            if (agent.Alarm is { Active: true }) // another guard beat this guard to it
            {
                agent.Memory.MarkAlarmSought();
                return ActionStatus.Succeeded;
            }

            if (_target == null) return GiveUp(agent);

            if (GuardAgent.IsAdjacent(agent.Motor.Cell, _target.Cell))
            {
                agent.Motor.Stop();
                _target.Activate(_contactCell, _contactHeading);
                agent.Memory.MarkAlarmSought();
                return ActionStatus.Succeeded;
            }

            if (agent.Motor.HasRoute && !agent.Motor.Blocked) return ActionStatus.Running; // still walking

            // The way shut mid-walk, or the walk stalled short of the switch.
            // One switch going out of reach doesn't mean the alarm can't be raised, so try again from here.
            return Target(agent) ? ActionStatus.Running : GiveUp(agent);
        }

        // Routes to the nearest switch. False when this guard can reach none.
        private bool Target(GuardAgent agent)
        {
            _target = agent.Alarm?.NearestSwitch(agent.Motor.Cell, agent.Pathfinder, agent);
            return _target != null && agent.Motor.SetGoal(_target.Cell);
        }

        // Nowhere to raise it: mark the guard done so it stops trying and drops back to patrol.
        private static ActionStatus GiveUp(GuardAgent agent)
        {
            agent.Memory.MarkAlarmSought();
            return ActionStatus.Failed;
        }
    }
}