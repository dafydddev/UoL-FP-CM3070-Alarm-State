using Simulation;

namespace Generation.Cells
{
    public interface IExitHandler
    {
        void OnExited(Actor mover);
    }
}