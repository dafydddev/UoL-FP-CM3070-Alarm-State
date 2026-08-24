using Simulation;

namespace Generation.Cells
{
    // Cell content that reacts when an actor uses it.
    public interface IUseHandler
    {
        // Whether using it now would activate it, asked before the key is pressed.
        bool CanUse(Actor user);

        // Returns false if there was nothing to activate, which frees the use for something else.
        bool OnUsed(Actor user);
    }
}
