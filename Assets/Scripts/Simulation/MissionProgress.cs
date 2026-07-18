using System;

namespace Simulation
{
    // Per-level mission progress. Rebuilt with the WorldContext each level,
    // so it resets automatically whenever a new level is generated.
    public sealed class MissionProgress
    {
        // True once the primary objective has been completed this level.
        // The exit stays locked until this flips.
        public bool PrimaryComplete { get; private set; }

        // Raised the moment the primary objective is completed.
        public event Action PrimaryCompleted;

        public void CompletePrimary()
        {
            if (PrimaryComplete) return;
            PrimaryComplete = true;
            PrimaryCompleted?.Invoke();
        }
    }
}
