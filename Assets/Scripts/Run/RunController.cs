using System.Collections;
using Analytics;
using Audio;
using Effects;
using Entities;
using Entities.Objectives;
using Generation.Facility;
using Generation.Tiles;
using Menu;
using Player;
using Settings;
using Simulation;
using Tutorials;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Run
{
    // Drives level progression. Builds starting the level and advances level each time the player reaches an exit.
    [RequireComponent(typeof(FacilityOrchestrator))]
    public class RunController : MonoBehaviour
    {
        [Header("Run Options")] 
        [SerializeField, Min(1)] private int startLevel = 1;
        [SerializeField, Min(1)] private int totalLevels = 10;
        [SerializeField] private RunDifficulty @default; // used when entering the scene directly
        [SerializeField] private TileLayoutStyle defaultLayoutStyle = TileLayoutStyle.Spine;
        [SerializeField] private ScreenWipeEffect wipeEffect;
        [SerializeField] private AudioFader audioFader;
        [SerializeField] private ResultsController resultsController;

        // Reported for a level nobody noticed the player on.
        private const float NoAlarm = -1f;

        private FacilityOrchestrator _facility;

        private RunContext _run;

        // Alarms raised since this level was built.
        private int _alarmsThisLevel;

        // Seconds of play since this level was built, and how far in its first alarm went up.
        private float _levelElapsed;
        private float _firstAlarmAt;

        private FacilityOrchestrator FacilityOrchestrator => _facility ??= GetComponent<FacilityOrchestrator>();

        private void Start()
        {
            GameLock.Clear();
            InputCapture.Clear();
            _run = RunContext.Pending ?? new RunContext(@default, startLevel, totalLevels, defaultLayoutStyle);
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
            foreach (var itemType in owned) loadout.Add(itemType);
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
            PauseMenu.Quit += OnQuit;
        }

        private void OnDisable()
        {
            Exit.Reached -= NextLevel;
            PlayerHealth.OnHealthChanged -= OnHealthChanged;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            SecondaryObjective.Completed -= OnSecondaryCompleted;
            PauseMenu.Quit -= OnQuit;
        }

        // The level's clock only runs while the world does, so a wipe, a tally or a pause doesn't count as play.
        private void Update()
        {
            if (!GameLock.Locked) _levelElapsed += Time.deltaTime;
        }

        // Counts and reports the alarm going up.
        private void OnAlarmChanged(bool active)
        {
            if (!active || _run == null) return;
            _alarmsThisLevel++;
            if (_alarmsThisLevel == 1) _firstAlarmAt = _levelElapsed;
            Telemetry.AlarmRaised(_run, _levelElapsed);
        }

        // Completing an objective adds its reward to the run's pending total.
        private void OnPrimaryCompleted() => _run.AwardPrimaryObjective(_run.PrimaryObjectiveReward);

        private void OnSecondaryCompleted() => _run.AwardSecondaryObjective(_run.SecondaryObjectiveReward);

        private void NextLevel()
        {
            // Record against the level just finished, before Advance moves the counter on.
            Telemetry.LevelCompleted(_run, _levelElapsed, _firstAlarmAt);
            RecordForm();
            // Fade out the level's music.
            if (audioFader) audioFader.FadeOut();
            // Nothing past the final level: clearing it ends the run a winner.
            if (!_run.Advance())
            {
                StartCoroutine(CompleteRun());
                return;
            }
            CarryOver();
            StartCoroutine(AdvanceLevel());
        }

        // Quitting from the pause menu abandons the run the same as a death does.
        private void OnQuit() => StartCoroutine(EndRun());

        // Carries the cleared level's player into the next one: the hearts they survived on and their inventory
        private void CarryOver()
        {
            var player = FacilityOrchestrator.World?.Player;
            if (!player) return;

            var loadout = new RunLoadout();
            if (player.TryGetComponent(out PlayerInventory inventory))
            {
                foreach (var itemType in inventory.Types) loadout.Add(itemType);
                loadout.Selection = inventory.Selected;
            }

            if (player.TryGetComponent(out PlayerHealth health)) loadout.StartingHearts = health.Hearts;

            RunLoadout.Pending = loadout;
        }

        // Stores how the finished level went: the hearts the player ended it on, and how many alarms they set off.
        private void RecordForm()
        {
            var player = FacilityOrchestrator.World?.Player;
            if (player && player.TryGetComponent(out PlayerHealth health))
                _run.Performance.RecordLevel(health.Hearts, health.MaxHearts, _alarmsThisLevel);
        }

        // Losing the last heart ends the run. PlayerHealth fires makes sure to only fire this once.
        // So that the run can't be ended twice by arrests landing in the same burst of ticks.
        private void OnHealthChanged(int current, int _)
        {
            if (current == 0) StartCoroutine(EndRun());
        }

        // The first level: freeze the sim, wipe to black, build behind it, reveal, then release our hold.
        private IEnumerator BuildLevel()
        {
            GameLock.Acquire();
            try
            {
                if (wipeEffect) yield return wipeEffect.Close();
                GenerateLevel();
                if (wipeEffect) yield return wipeEffect.Open();
            }
            finally
            {
                GameLock.Release();
            }
        }

        // Each later level: wipe down, reveal the cleared level's tally, then build the next behind the black.
        private IEnumerator AdvanceLevel()
        {
            GameLock.Acquire();
            try
            {
                if (wipeEffect) yield return wipeEffect.Close();
                if (resultsController)
                {
                    resultsController.Show(_run, "Level Complete", ResultsScreen.LevelComplete);
                    if (wipeEffect) yield return wipeEffect.Open();
                    yield return resultsController.RunTally(_run);
                    if (wipeEffect) yield return wipeEffect.Close();
                    resultsController.Hide();
                }

                GenerateLevel();
                if (wipeEffect) yield return wipeEffect.Open();
                // Fade in the level's music.
                if (audioFader) audioFader.FadeIn();
            }
            finally
            {
                GameLock.Release();
            }
        }

        // Builds the level for the current run state and re-arms this level's primary-objective award.
        private void GenerateLevel()
        {
            _alarmsThisLevel = 0;
            _levelElapsed = 0f;
            _firstAlarmAt = NoAlarm;
            FacilityOrchestrator.Generate(_run);
            FacilityOrchestrator.World.Mission.PrimaryCompleted += OnPrimaryCompleted;
            Telemetry.LevelStarted(_run);
            Tutorial.ShowOnce(TutorialTopic.FirstLevel);
        }

        // The cleared run: the completion bonus lands, and the inventory cashes which items weren't used.
        // Read before the level is torn down, the same as the carry-over between levels.
        private IEnumerator CompleteRun()
        {
            Telemetry.RunCompleted(_run);
            _run.AwardRunCompleted(_run.RunCompletedReward);
            var player = FacilityOrchestrator.World?.Player;
            if (player && player.TryGetComponent(out PlayerInventory inventory)) _run.AwardUnusedItems(inventory.CashInValue);
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            if (resultsController)
            {
                resultsController.Show(_run, "Run Complete", ResultsScreen.RunComplete);
                if (wipeEffect) yield return wipeEffect.Open();
                yield return resultsController.RunBanked(_run);
                if (wipeEffect) yield return wipeEffect.Close();
                resultsController.Hide();
            }
            CurrencySettings.Balance += _run.PendingCurrency;
            CurrencySettings.Save();
            SceneManager.LoadScene("Main Menu");
        }

        // The lost run, whether the player died or quit.
        private IEnumerator EndRun()
        {
            Telemetry.LevelFailed(_run, _levelElapsed, _firstAlarmAt);
            GameLock.Acquire();
            if (wipeEffect) yield return wipeEffect.Close();
            if (audioFader) audioFader.FadeOut();
            if (resultsController)
            {
                resultsController.Show(_run, "Run Failed", ResultsScreen.RunFailed);
                if (wipeEffect) yield return wipeEffect.Open();
                yield return resultsController.RunForfeit(_run);
                if (wipeEffect) yield return wipeEffect.Close();
                resultsController.Hide();
            }

            SceneManager.LoadScene("Main Menu");
        }
    }
}