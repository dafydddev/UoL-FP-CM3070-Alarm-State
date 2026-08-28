using Guards.Goap;
using UnityEngine;

namespace Guards.Actions
{
    // Closes in on the player while they stay in sight, re-routing whenever they move.
    // Succeeds once within arrest reach. Fails if sight is lost or no path exists.
    // Losing sight of the player also leaves a last-seen lead behind, so the investigate goal can take over.
    public sealed class ChasePlayerAction : GoapAction
    {
        private Vector2Int _target;

        public ChasePlayerAction()
        {
            Preconditions = WorldState.Empty.With(Fact.SeesPlayer, true);
            Effects = WorldState.Empty.With(Fact.AtPlayer, true);
        }

        public override void OnEnter(GuardAgent agent) => _target = agent.Motor.Cell;

        public override ActionStatus Run(GuardAgent agent)
        {
            if (!agent.Memory.SeesPlayer) return ActionStatus.Failed;

            var playerCell = agent.Memory.PlayerCell;
            if (GuardAgent.IsAdjacent(agent.Motor.Cell, playerCell))
            {
                agent.Motor.Stop();
                return ActionStatus.Succeeded;
            }

            // Only re-path when the player has actually changed cell.
            if (playerCell != _target || !agent.Motor.HasRoute)
            {
                if (!agent.Motor.SetGoal(playerCell)) return ActionStatus.Failed;
                _target = playerCell;
            }

            return ActionStatus.Running;
        }
    }
}