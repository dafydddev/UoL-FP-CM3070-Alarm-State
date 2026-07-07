using Simulation;

namespace Generation.Cells
{
    // Cell content that can refuse an actor's entry.
    public interface IEntryBlocker
    {
        bool BlocksEntry(Actor mover);
    }
}