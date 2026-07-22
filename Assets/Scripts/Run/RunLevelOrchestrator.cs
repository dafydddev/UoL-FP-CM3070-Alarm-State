using System.Collections;
using Analytics;
using Effects;
using Entities;
using Entities.Objectives;
using Generation.Facility;
using Generation.Tiles;
using Player;
using Settings;
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

        [Header("Rewards")]
        [SerializeField, Min(0)] private int primaryObjectiveReward = 100;
        [SerializeField, Min(0)] private int secondaryObjectiveReward = 50;
        [SerializeField, Min(0)] private int levelClearedReward = 25;

        private FacilityOrchestrator _facility;

        private RunContext _run;

        private FacilityOrchestrator FacilityOrchestrator => _facility ??= GetComponent<FacilityOrchestrator>();

        private void Start()
        {
            GameLock.Clear();
            _run = RunContext.Pending ?? new RunContext(@default, defaultStartLevel, defaultTotalLevels, defaultLayoutStyle);
            RunContext.Pending = null;
            OpenLoadout();
            StartCoroutine(BuildLevel());
        }

        // Opens this run's inventory from the items bought in the shop, then spends them.
        // A run entered without a shop (a direct scene load) opens empty.
        private static void OpenLoadout()
        {
            var owned = SaveSystem.Data.ownedItems;
            if (owned.Count == 0) return;

            var loadout = new RunLoadout();
            foreach (var kind in owned) loadout.Add(kind);
            RunLoadout.Pending = loadout;

            owned.Clear();
            SaveSystem.Save();
        }

        private void OnEnable()
        {
            Exit.Reached += NextLevel;
            PlayerHealth.OnHealthChanged += OnHealthChanged;
            AlarmState.ActiveChanged += OnAlarmChanged;
            SecondaryObjective.Completed += OnSecondaryCompleted;
        }

        private void OnDisable()
        {
            Exit.Reached -= NextLevel;
            PlayerHealth.OnHealthChanged -= OnHealthChanged;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            SecondaryObjective.Completed -= OnSecondaryCompleted;
        }

        // Records each raise against the current run (the off and per-level reset edges carry no data).
        private void OnAlarmChanged(bool active)
        {
            if (active && _run != null) Telemetry.AlarmRaised(_run);
        }

        // Completing an objective adds its reward to the run's pending total.
        private void OnPrimaryCompleted() => _run.Award(primaryObjectiveReward);

        private void OnSecondaryCompleted() => _run.Award(secondaryObjectiveReward);

        private void NextLevel()
        {
            // Record against the level just finished, before Advance moves the counter on.
            Telemetry.LevelCompleted(_run);
            _run.Award(levelClearedReward);
            // Nothing past the final level: clearing it ends the run a winner.
            if (!_run.Advance())
            {
                StartCoroutine(CompleteRun());
                return;
            }

            CarryOver();
            StartCoroutine(BuildLevel());
        }

        // Carries the cleared level's player into the next one: the hearts they survived on and their inventory
        // Read before BuildLevel tears the level down and spawns their replacement.
        private void CarryOver()
        {
            var player = FacilityOrchestrator.World?.Player;
            if (!player) return;

            var loadout = new RunLoadout();
            if (player.TryGetComponent(out PlayerInventory inventory))
            {
                foreach (var kind in inventory.Kinds) loadout.Add(kind);
                loadout.Selection = inventory.Selected;
            }

            if (player.TryGetComponent(out PlayerHealth health)) loadout.StartingHearts = health.Hearts;

            RunLoadout.Pending = loadout;
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
                // The mission is rebuilt with each level; award this level's primary completion.
                FacilityOrchestrator.World.Mission.PrimaryCompleted += OnPrimaryCompleted;
                Telemetry.LevelStarted(_run);
                if (wipeEffect) yield return wipeEffect.Open();
            }
            finally
            {
                GameLock.Release();
            }
        }

        // The cleared run: the same exit as a failed one, so the hold and wipe behave identically.
        // It banks the run's pending currency here, where a failed run discards it.
        private IEnumerator CompleteRun()
        {
            Telemetry.RunCompleted(_run);
            CurrencySettings.Balance += _run.PendingCurrency;
            CurrencySettings.Save();
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            SceneManager.LoadScene("Main Menu");
        }

        // The failed run: freeze the sim, wipe to black, then hand back to the menu.
        private IEnumerator EndRun()
        {
            Telemetry.LevelFailed(_run);
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            SceneManager.LoadScene("Main Menu");
        }
    }
}