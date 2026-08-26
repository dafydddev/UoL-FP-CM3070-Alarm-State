namespace Entities.Objectives
{
    // The mission's terminal objective.
    // Completing it marks the mission done, which the exit checks before it will let the player through.
    public class PrimaryObjective : Objective
    {
        public override MiniGameType Game => MiniGameType.Pipes;

        protected override void OnWon() => World.Mission.CompletePrimary();
    }
}