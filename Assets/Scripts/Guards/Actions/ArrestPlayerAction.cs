using Guards.Goap;

namespace Guards.Actions
{
    // Holds the caught player for a moment, then raises the arrest.
    // A cooldown in memory marks the catch as recent, so the chase goal stands down for a while,
    // instead of re-arresting every tick (the legacy code faked this with a timed failure).
    public sealed class ArrestPlayerAction : GoapAction
    {
        private const int HoldTicks = 3;
        private const int CooldownTicks = 30;

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
            // The player slipped out of reach — fall back to the plan's chase step via a replan.
            if (!agent.Memory.SeesPlayer || !GuardAgent.IsAdjacent(agent.Motor.Cell, agent.Memory.PlayerCell))
                return ActionStatus.Failed;

            if (--_hold > 0) return ActionStatus.Running;

            GuardAgent.RaisePlayerCaught();
            agent.Memory.BeginArrestCooldown(CooldownTicks);
            return ActionStatus.Succeeded;
        }
    }
}