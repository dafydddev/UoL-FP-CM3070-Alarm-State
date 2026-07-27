using Guards.Goap;
using UnityEngine;

namespace Guards.Actions
{
    // Walks to the current lead (a last-seen position or a heard distraction),
    // looks around for a moment, then resolves it: a distraction found there is consumed, and the lead is cleared either way.
    // An unreachable lead is cleared too — a guard shouldn't obsess over a spot it can never reach.
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

        // How long the guard looks the lead over. A distraction says for itself,
        // so an upgraded one stalls the guard on its cell that much longer.
        private static int LingerFor(GuardMemory memory) =>
            memory.LeadItem ? memory.LeadItem.LingerTicks : DefaultLingerTicks;

        public override ActionStatus Run(GuardAgent agent)
        {
            var memory = agent.Memory;
            if (!memory.HasLead) return ActionStatus.Failed;

            // Route to the lead — again if a fresher lead replaced it mid-walk
            // (e.g. a louder noise, or the player seen and lost somewhere new).
            if (!_started || memory.LeadCell != _goalCell)
            {
                _started = true;
                _goalCell = memory.LeadCell;
                _linger = LingerFor(memory);
                if (!agent.Motor.SetGoal(_goalCell))
                {
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

            // At the lead: look around, then resolve it.
            if (_linger-- > 0) return ActionStatus.Running;

            if (memory.LeadItem) memory.LeadItem.Consume();
            else if (memory.LeadIsPlayerTrail) memory.MarkTrailLost(); // trail followed to its end, nothing found
            memory.ClearLead();
            return ActionStatus.Succeeded;
        }
    }
}