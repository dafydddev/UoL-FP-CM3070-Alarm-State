using System;
using System.Collections;
using Run;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Which end the results panel is reporting on, and so which of its rows have anything to say.
    public enum ResultsScreen
    {
        LevelComplete,
        RunComplete,
        RunFailed
    }

    // The end-of-level results screen: a passive panel the run orchestrator reveals between wipes.
    // It breaks the run's takings down, counts them into a total, and reports when the player continues.
    public class ResultsController : MonoBehaviour
    {
        // What one screen reports: the rows it carries, and the figure its total opens on.
        private struct ResultsView
        {
            public bool ShowBreakdown;
            public bool ShowRunBonus;
            public bool ShowUnusedItems;
            public bool ShowBalance;
            public int InitialTotal;
        }

        [Header("Results screen scaffold")]
        [SerializeField] private MenuPanel resultsMenu;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text headingLabel;

        [Header("Result screen data")]
        [SerializeField] private GameObject primaryObjectiveRow;
        [SerializeField] private TMP_Text primaryObjectiveLabel;
        [SerializeField] private GameObject secondaryObjectiveRow;
        [SerializeField] private TMP_Text secondaryObjectiveLabel;
        [SerializeField] private GameObject runCompleteRow;
        [SerializeField] private TMP_Text runCompleteLabel;
        [SerializeField] private GameObject unusedItemsRow;
        [SerializeField] private TMP_Text unusedItemsLabel;
        [SerializeField] private TMP_Text totalLabel;
        [SerializeField] private GameObject balanceRow;
        [SerializeField] private TMP_Text balanceLabel;

        [SerializeField, Min(0f)] private float countSeconds = 0.5f;

        private bool _continued;
        private bool _counting;

        private void OnEnable() => continueButton.onClick.AddListener(OnContinue);

        private void OnDisable() => continueButton.onClick.RemoveListener(OnContinue);

        // Mid-count the button skips the tally to its finished figures; only a press after that continues.
        private void OnContinue()
        {
            if (_counting) _counting = false;
            else _continued = true;
        }

        private static ResultsView ViewFor(ResultsScreen screen, RunContext run) => screen switch
        {
            ResultsScreen.LevelComplete => new ResultsView
            {
                ShowBreakdown = true, ShowRunBonus = false, ShowUnusedItems = false, ShowBalance = false,
                InitialTotal = 0
            },
            ResultsScreen.RunComplete => new ResultsView
            {
                ShowBreakdown = true, ShowRunBonus = true, ShowUnusedItems = true, ShowBalance = true,
                InitialTotal = 0
            },
            // The forfeit drains rather than climbs, so it opens on the full purse.
            ResultsScreen.RunFailed => new ResultsView
            {
                ShowBreakdown = false, ShowRunBonus = false, ShowUnusedItems = false, ShowBalance = false,
                InitialTotal = run.PendingCurrency
            },
            _ => throw new ArgumentOutOfRangeException(nameof(screen), screen, null)
        };

        // Shows the panel with the heading and whatever this screen breaks down, the total still to be counted.
        public void Show(RunContext run, string heading, ResultsScreen screen)
        {
            _continued = false;
            _counting = true;
            if (headingLabel) headingLabel.text = heading;

            var view = ViewFor(screen, run);
            primaryObjectiveRow.SetActive(view.ShowBreakdown);
            secondaryObjectiveRow.SetActive(view.ShowBreakdown);
            runCompleteRow.SetActive(view.ShowRunBonus);
            unusedItemsRow.SetActive(view.ShowUnusedItems);
            balanceRow.SetActive(view.ShowBalance);

            if (view.ShowBreakdown)
            {
                SetLabel(primaryObjectiveLabel, run.PrimaryObjectiveEarnings);
                SetLabel(secondaryObjectiveLabel, run.SecondaryObjectiveEarnings);
            }

            if (view.ShowRunBonus) SetLabel(runCompleteLabel, run.RunCompletedEarnings);
            if (view.ShowUnusedItems) SetLabel(unusedItemsLabel, run.UnusedItemEarnings);
            if (view.ShowBalance) SetLabel(balanceLabel, CurrencySettings.Balance);
            SetLabel(totalLabel, view.InitialTotal);

            resultsMenu.SetActive(true);
        }

        public void Hide() => resultsMenu.SetActive(false);

        // A cleared level: the tally counts up and stands there until the player continues.
        public IEnumerator RunTally(RunContext run)
        {
            yield return Count(totalLabel, 0, run.PendingCurrency);
            yield return WaitForContinue();
        }

        // A completed run: the tally counts up, then the balance climbs by it.
        // One skip covers both, since the press clears the flag they share.
        public IEnumerator RunBanked(RunContext run)
        {
            yield return Count(totalLabel, 0, run.PendingCurrency);
            var balance = CurrencySettings.Balance;
            yield return Count(balanceLabel, balance, balance + run.PendingCurrency);
            yield return WaitForContinue();
        }

        // A lost run: the tally drains back to nothing.
        public IEnumerator RunForfeit(RunContext run)
        {
            yield return Count(totalLabel, run.PendingCurrency, 0);
            yield return WaitForContinue();
        }

        // The counting is over, so the button goes back to meaning continue.
        private IEnumerator WaitForContinue()
        {
            _counting = false;
            yield return new WaitUntil(() => _continued);
        }

        // Counts a label from one figure to another over countSeconds, or lands straight on it once skipped.
        // Unscaled time so it counts at a steady rate whatever the sim freeze does to the time scale.
        private IEnumerator Count(TMP_Text label, int from, int to)
        {
            if (!label) yield break;
            // A figure that doesn't move has nothing to count, so it doesn't hold the screen.
            if (from == to)
            {
                SetLabel(label, to);
                yield break;
            }

            for (var t = 0f; _counting && t < countSeconds; t += Time.unscaledDeltaTime)
            {
                SetLabel(label, Mathf.RoundToInt(Mathf.Lerp(from, to, t / countSeconds)));
                yield return null;
            }

            SetLabel(label, to);
        }

        private static void SetLabel(TMP_Text label, int value)
        {
            if (label) label.text = value.ToString();
        }
    }
}