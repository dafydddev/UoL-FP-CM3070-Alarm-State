using Simulation;

namespace Generation.Cells
{
    // Cell content that reacts when an actor leaves.
    public interface IExitHandler
    {
        void OnExited(Actor mover);
    }
}