using Entities;
using Generation.Facility;
using Run;
using UnityEngine;

namespace Generation
{
    // Drives level progression:
    // Builds the starting level, then advances one level each time the player reaches an exit.
    // Difficulty scales with the level number, which the facility orchestrator handles.
    [RequireComponent(typeof(FacilityOrchestrator))]
    public class LevelOrchestrator : MonoBehaviour
    {
        [SerializeField, Min(1)] private int startLevel = 1;
        [SerializeField, Min(1)] private int totalLevels = 10;

        private FacilityOrchestrator _facility;

        private RunContext _run;

        private FacilityOrchestrator FacilityOrchestrator => _facility ??= GetComponent<FacilityOrchestrator>();

        private void Start()
        {
            _run = new RunContext(startLevel, totalLevels);
            FacilityOrchestrator.Generate(_run);
        }

        private void OnEnable() => Exit.Reached += NextLevel;
        private void OnDisable() => Exit.Reached -= NextLevel;

        private void NextLevel()
        {
            // run complete; nothing past the final level
            if (!_run.Advance()) return; 
            FacilityOrchestrator.Generate(_run);
        }
    }
}