using Simulation;

namespace Generation.Cells
{
    // Cell content that reacts when an actor uses it.
    public interface IUseHandler
    {
        void OnUsed(Actor user);
    }
}
