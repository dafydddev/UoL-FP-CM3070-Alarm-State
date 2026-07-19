using Run;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Analytics
{
    // Sends the run's core beats to Unity Analytics.
    // Initialises itself with the app and if the service never comes up, the record calls quietly do nothing.
    public static class Telemetry
    {
        // False until Unity Services has initialised; the SDK throws before then.
        private static bool _ready;

        [RuntimeInitializeOnLoadMethod]
        private static async void Init()
        {
            // Catches everything: async void lets exceptions escape to the engine,
            // and no analytics failure is worth interrupting play over.
            try
            {
                await UnityServices.InitializeAsync();
                // Collection is granted at startup, switches the analytics SDK on.
                // Important: game is shared with a form that confirms that this data is gathered.
                EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted });

                _ready = true;
                Application.quitting += OnQuitting;
                AnalyticsService.Instance.RecordEvent("gameStarted");
            }
            catch
            {
                // no service, no telemetry; the game plays on regardless
            }
        }

        public static void LevelStarted(RunContext run) => Record("levelStarted", run);

        public static void LevelCompleted(RunContext run) => Record("levelCompleted", run);

        public static void LevelFailed(RunContext run) => Record("levelFailed", run);

        // Every level event carries the same snapshot of the run.
        private static void Record(string name, RunContext run)
        {
            if (!_ready) return;
            AnalyticsService.Instance.RecordEvent(new CustomEvent(name)
            {
                { "level", run.CurrentLevel },
                { "totalLevels", run.TotalLevels },
                { "difficulty", run.Profile.label },
                { "layout", run.LayoutStyle.ToString() },
            });
        }

        // Push anything still buffered before the process goes away.
        private static void OnQuitting()
        {
            AnalyticsService.Instance.RecordEvent("applicationQuit");
            AnalyticsService.Instance.Flush();
        }
    }
}
