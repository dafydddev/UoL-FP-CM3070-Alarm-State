using System.Collections;
using Analytics;
using Effects;
using Entities;
using Generation.Facility;
using Generation.Tiles;
using Player;
using Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Run
{
    // Drives level progression:
    // Builds the starting level, then advances one level each time the player reaches an exit.
    // Difficulty scales with the level number, which the facility orchestrator handles.
    [RequireComponent(typeof(FacilityOrchestrator))]
    public class RunLevelOrchestrator : MonoBehaviour
    {
        [Header("Run Options")]
        [SerializeField, Min(1)] private int defaultStartLevel = 1;
        [SerializeField, Min(1)] private int defaultTotalLevels = 10;
        [SerializeField] private RunDifficulty @default; // used when entering the scene directly
        [SerializeField] private TileLayoutStyle defaultLayoutStyle = TileLayoutStyle.Spine;
        [SerializeField] private ScreenWipeEffect wipeEffect;

        private FacilityOrchestrator _facility;

        private RunContext _run;

        private FacilityOrchestrator FacilityOrchestrator => _facility ??= GetComponent<FacilityOrchestrator>();

        private void Start()
        {
            GameLock.Clear();
            _run = RunContext.Pending ?? new RunContext(@default, defaultStartLevel, defaultTotalLevels, defaultLayoutStyle);
            RunContext.Pending = null;
            StartCoroutine(BuildLevel());
        }

        private void OnEnable()
        {
            Exit.Reached += NextLevel;
            PlayerHealth.OnHealthChanged += OnHealthChanged;
            AlarmState.ActiveChanged += OnAlarmChanged;
        }

        private void OnDisable()
        {
            Exit.Reached -= NextLevel;
            PlayerHealth.OnHealthChanged -= OnHealthChanged;
            AlarmState.ActiveChanged -= OnAlarmChanged;
        }

        // Records each raise against the current run (the off and per-level reset edges carry no data).
        private void OnAlarmChanged(bool active)
        {
            if (active && _run != null) Telemetry.AlarmRaised(_run);
        }

        private void NextLevel()
        {
            // Record against the level just finished, before Advance moves the counter on.
            Telemetry.LevelCompleted(_run);
            // Nothing past the final level: clearing it ends the run a winner.
            if (!_run.Advance())
            {
                StartCoroutine(CompleteRun());
                return;
            }

            StartCoroutine(BuildLevel());
        }

        // Losing the last heart ends the run. PlayerHealth fires this at most once at zero,
        // so the run can't be ended twice by arrests landing in the same burst of ticks.
        private void OnHealthChanged(int current, int _)
        {
            if (current == 0) StartCoroutine(EndRun());
        }

        // Freeze the sim, wipe to black, rebuild, reveal, then release our hold.
        private IEnumerator BuildLevel()
        {
            GameLock.Acquire();
            try
            {
                if (wipeEffect) yield return wipeEffect.Close();
                FacilityOrchestrator.Generate(_run);
                Telemetry.LevelStarted(_run);
                if (wipeEffect) yield return wipeEffect.Open();
            }
            finally
            {
                GameLock.Release();
            }
        }

        // The cleared run: the same exit as a failed one, so the hold and wipe behave identically.
        // Stage 5 will commit the run's unlocks here, where a failed run discards them.
        private IEnumerator CompleteRun()
        {
            Telemetry.RunCompleted(_run);
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            SceneManager.LoadScene("Main Menu");
        }

        // The failed run: freeze the sim, wipe to black, then hand back to the menu.
        // The hold is deliberately never released — the menu doesn't tick the sim,
        // and re-entering the gameplay scene clears leaked holds (see GameLock.Clear).
        private IEnumerator EndRun()
        {
            Telemetry.LevelFailed(_run);
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            SceneManager.LoadScene("Main Menu");
        }
    }
}