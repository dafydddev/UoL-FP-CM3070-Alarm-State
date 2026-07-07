using Simulation;

namespace Generation.Cells
{
    // Cell content that reacts when an actor enters.
    public interface IEnterHandler
    {
        void OnEntered(Actor mover);
    }
}