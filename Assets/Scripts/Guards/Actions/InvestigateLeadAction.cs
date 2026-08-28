using Guards.Goap;
using UnityEngine;

namespace Guards.Actions
{
    // Walks to the current lead, a last-seen position or a heard distraction.
    // Looks around for a moment, then resolves it. A distraction found there is consumed. The lead is cleared either way.
    public sealed class InvestigateLeadAction : GoapAction
    {
        private const int DefaultLingerTicks = 6;

        private Vector2Int _goalCell;
        private bool _started;
        private int _linger;

        public InvestigateLeadAction()
        {
            Preconditions = WorldState.Empty.With(Fact.HasLead, true);
            Effects = WorldState.Empty.With(Fact.HasLead, false);
        }

        public override void OnEnter(GuardAgent agent)
        {
            _started = false;
            _linger = LingerFor(agent.Memory);
        }

        // How long the guard looks the lead over.
        // A distraction has its own tick count, so an upgraded one stalls the guard for a longer time.
        private static int LingerFor(GuardMemory memory) =>
            memory.LeadItem ? memory.LeadItem.LingerTicks : DefaultLingerTicks;

        public override ActionStatus Run(GuardAgent agent)
        {
            var memory = agent.Memory;
            if (!memory.HasLead) return ActionStatus.Failed;

            // Route to the lead.
            // Re-route if a fresher lead replaced it mid-walk (e.g. the player seen).
            if (!_started || memory.LeadCell != _goalCell)
            {
                _started = true;
                _goalCell = memory.LeadCell;
                _linger = LingerFor(memory);
                if (!agent.Motor.SetGoal(_goalCell))
                {
                    // Unreachable, so the lead is dropped rather than left to be replanned onto forever.
                    memory.ClearLead();
                    return ActionStatus.Failed;
                }
            }

            if (agent.Motor.Blocked)
            {
                memory.ClearLead();
                return ActionStatus.Failed;
            }

            if (agent.Motor.HasRoute) return ActionStatus.Running; // still walking

            // At the lead. Look around, then resolve it.
            if (_linger-- > 0) return ActionStatus.Running;

            if (memory.LeadItem) memory.LeadItem.Consume();
            else if (memory.LeadIsPlayerTrail) memory.MarkTrailLost(); // trail followed to its end, nothing found
            memory.ClearLead();
            return ActionStatus.Succeeded;
        }
    }
}