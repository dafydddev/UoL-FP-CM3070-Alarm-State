using System.Collections;
using Effects;
using Entities;
using Generation.Facility;
using Simulation;
using UnityEngine;

namespace Run
{
    // Drives level progression:
    // Builds the starting level, then advances one level each time the player reaches an exit.
    // Difficulty scales with the level number, which the facility orchestrator handles.
    [RequireComponent(typeof(FacilityOrchestrator))]
    public class LevelOrchestrator : MonoBehaviour
    {
        [SerializeField, Min(1)] private int defaultStartLevel = 1;
        [SerializeField, Min(1)] private int defaultTotalLevels = 10;
        [SerializeField] private RunDifficultyProfile defaultProfile; // used when entering the scene directly
        [SerializeField] private ScreenWipe wipe;

        private FacilityOrchestrator _facility;

        private RunContext _run;

        private FacilityOrchestrator FacilityOrchestrator => _facility ??= GetComponent<FacilityOrchestrator>();

        private void Start()
        {
            GameLock.Clear();
            _run = RunContext.Pending ?? new RunContext(defaultProfile, defaultStartLevel, defaultTotalLevels);
            RunContext.Pending = null;
            StartCoroutine(BuildLevel());
        }

        private void OnEnable() => Exit.Reached += NextLevel;
        private void OnDisable() => Exit.Reached -= NextLevel;

        private void NextLevel()
        {
            // run complete; nothing past the final level
            if (!_run.Advance()) return;
            StartCoroutine(BuildLevel());
        }

        // Freeze the sim, wipe to black, rebuild, reveal, then release our hold.
        private IEnumerator BuildLevel()
        {
            GameLock.Acquire();
            try
            {
                if (wipe) yield return wipe.Close();
                FacilityOrchestrator.Generate(_run);
                if (wipe) yield return wipe.Open();
            }
            finally
            {
                GameLock.Release();
            }
        }
    }
}