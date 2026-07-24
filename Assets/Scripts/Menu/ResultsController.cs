using System.Collections;
using Run;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // The end-of-level results screen: a passive panel the run orchestrator reveals between wipes.
    // It breaks the run's takings down, counts them into a total, and reports when the player continues.
    public class ResultsController : MonoBehaviour
    {
        [Header("UI Game Objects")]
        [SerializeField] private MenuPanel resultsMenu;
        [SerializeField] private Button continueButton;

        [Header("Takings Labels")]
        [SerializeField] private TMP_Text headingLabel;
        [SerializeField] private TMP_Text levelClearedLabel;
        [SerializeField] private TMP_Text primaryObjectiveLabel;
        [SerializeField] private TMP_Text secondaryObjectiveLabel;
        [SerializeField] private TMP_Text totalLabel;
        [SerializeField] private TMP_Text balanceLabel;

        [SerializeField, Min(0f)] private float countSeconds = 0.5f;

        private bool _continued;

        private void OnEnable() => continueButton.onClick.AddListener(OnContinue);

        private void OnDisable() => continueButton.onClick.RemoveListener(OnContinue);

        private void OnContinue() => _continued = true;

        // Shows the panel with the heading and the run's takings broken out, the total still to be counted in.
        public void Show(RunContext run, string heading)
        {
            _continued = false;
            if (headingLabel) headingLabel.text = heading;
            SetLabel(levelClearedLabel, run.LevelClearedEarnings);
            SetLabel(primaryObjectiveLabel, run.PrimaryObjectiveEarnings);
            SetLabel(secondaryObjectiveLabel, run.SecondaryObjectiveEarnings);
            SetLabel(totalLabel, 0);
            SetLabel(balanceLabel, CurrencySettings.Balance);
            resultsMenu.SetActive(true);
        }

        public void Hide() => resultsMenu.SetActive(false);

        // A cleared level: the tally counts up and stands there until the player continues.
        public IEnumerator RunTally(RunContext run)
        {
            yield return Count(totalLabel, 0, run.PendingCurrency);
            yield return new WaitUntil(() => _continued);
        }

        // A completed run: the tally counts up, then the balance climbs by it.
        public IEnumerator RunBanked(RunContext run)
        {
            yield return Count(totalLabel, 0, run.PendingCurrency);
            var balance = CurrencySettings.Balance;
            yield return Count(balanceLabel, balance, balance + run.PendingCurrency);
            yield return new WaitUntil(() => _continued);
        }

        // A lost run: the tally counts up, then drains back to nothing.
        public IEnumerator RunForfeit(RunContext run)
        {
            yield return Count(totalLabel, 0, run.PendingCurrency);
            yield return Count(totalLabel, run.PendingCurrency, 0);
            yield return new WaitUntil(() => _continued);
        }

        // Counts a label from one figure to another over countSeconds.
        // Unscaled time so it counts at a steady rate whatever the sim freeze does to the time scale.
        private IEnumerator Count(TMP_Text label, int from, int to)
        {
            if (!label) yield break;
            for (var t = 0f; t < countSeconds; t += Time.unscaledDeltaTime)
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
