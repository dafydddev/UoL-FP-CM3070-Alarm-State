namespace Guards.Goap
{
    // The outcome of running an action for one tick: still working, finished, or no longer possible.
    public enum ActionStatus
    {
        Running,
        Succeeded,
        Failed
    }

    // One step a guard can take. Preconditions and Effects describe the action to the planner; OnEnter/Run execute it.
    // An action only reports on its own progress interruption by more important goals is the agent's job,
    // so actions never need to know about the goal hierarchy.
    // Instances are per-agent and may keep execution state across ticks (timers, indices).
    public abstract class GoapAction
    {
        public WorldState Preconditions { get; protected set; } = WorldState.Empty;
        public WorldState Effects { get; protected set; } = WorldState.Empty;

        // Planning cost; the planner prefers the cheapest total plan.
        public virtual int Cost => 1;

        // Called once when the action becomes the plan's current step.
        public virtual void OnEnter(GuardAgent agent)
        {
        }

        // Called once per agent tick while the action is current.
        public abstract ActionStatus Run(GuardAgent agent);
    }
}