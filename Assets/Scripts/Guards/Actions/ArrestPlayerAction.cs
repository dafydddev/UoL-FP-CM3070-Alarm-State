using Guards.Goap;

namespace Guards.Actions
{
    // Holds the caught player for a moment, then raises the arrest.
    // The player's iFrames (invincibility frames) decide which arrests actually cost a heart.
    public sealed class ArrestPlayerAction : GoapAction
    {
        private const int HoldTicks = 6;

        private int _hold;

        public ArrestPlayerAction()
        {
            Preconditions = WorldState.Empty.With(Fact.AtPlayer, true);
            Effects = WorldState.Empty.With(Fact.PlayerCaught, true);
        }

        public override void OnEnter(GuardAgent agent)
        {
            _hold = HoldTicks;
            agent.Motor.Stop();
        }

        public override ActionStatus Run(GuardAgent agent)
        {
            // The player slipped out of reach, fall back to the plan's chase step via a replan.
            if (!agent.Memory.SeesPlayer || !GuardAgent.IsAdjacent(agent.Motor.Cell, agent.Memory.PlayerCell))
                return ActionStatus.Failed;

            if (--_hold > 0) return ActionStatus.Running;

            GuardAgent.RaisePlayerCaught();
            return ActionStatus.Succeeded;
        }
    }
}