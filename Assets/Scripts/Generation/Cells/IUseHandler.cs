using Simulation;

namespace Generation.Cells
{
    // Cell content that reacts when an actor uses it.
    public interface IUseHandler
    {
        // Returns false if there was nothing to activate, which frees the use for something else.
        bool OnUsed(Actor user);
    }
}
