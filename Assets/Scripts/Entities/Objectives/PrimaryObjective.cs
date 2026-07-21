namespace Entities.Objectives
{
    // The mission's terminal objective.
    // Hacking it marks the mission done, which the exit checks before it will let the player through.
    public class PrimaryObjective : Objective
    {
        protected override void OnHacked() => World.Mission.CompletePrimary();
    }
}
