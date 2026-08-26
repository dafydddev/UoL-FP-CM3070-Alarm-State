namespace Simulation
{
    // An entity that has to be wired into the sim after being placed in the world.
    // Spawners that know the type they are placing just call Init on it directly.
    // This is for callers holding a mixed set of prefabs, such as an objective's reward table.
    public interface ISpawnedEntity
    {
        void Init(WorldContext world);
    }
}