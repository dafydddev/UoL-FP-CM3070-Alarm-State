using System.Collections;
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
        }

        private void OnDisable()
        {
            Exit.Reached -= NextLevel;
            PlayerHealth.OnHealthChanged -= OnHealthChanged;
        }

        private void NextLevel()
        {
            // run complete; nothing past the final level
            if (!_run.Advance()) return;
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
                if (wipeEffect) yield return wipeEffect.Open();
            }
            finally
            {
                GameLock.Release();
            }
        }

        // The failed run: freeze the sim, wipe to black, then hand back to the menu.
        // The hold is deliberately never released — the menu doesn't tick the sim,
        // and re-entering the gameplay scene clears leaked holds (see GameLock.Clear).
        private IEnumerator EndRun()
        {
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            SceneManager.LoadScene("Main Menu");
        }
    }
}