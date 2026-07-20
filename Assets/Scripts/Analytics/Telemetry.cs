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
        // Null until Unity Services has come up; record calls quietly no-op without it.
        private static IAnalyticsService _analytics;

        [RuntimeInitializeOnLoadMethod]
        private static async void Init()
        {
            try
            {
                await UnityServices.InitializeAsync();
                // Collection is granted at startup, switches the analytics SDK on.
                // Important: game is shared with a form that confirms that this data is gathered.
                EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted });

                // The SDK tracks the session itself (gameStarted, gameEnded on quit, final flush);
                _analytics = AnalyticsService.Instance;
            }
            catch
            {
                // no service, no telemetry; the game plays on regardless
            }
        }

        public static void LevelStarted(RunContext run) => Record("levelStarted", run);

        public static void LevelCompleted(RunContext run) => Record("levelCompleted", run);

        public static void LevelFailed(RunContext run) => Record("levelFailed", run);

        public static void RunCompleted(RunContext run) => Record("runCompleted", run);

        public static void AlarmRaised(RunContext run) => Record("alarmRaised", run);

        // Every level event carries the same snapshot of the run.
        // Once the SDK has shut down (quit, or leaving play mode) it drops the event on the floor itself.
        private static void Record(string name, RunContext run)
        {
            _analytics?.RecordEvent(new CustomEvent(name)
            {
                { "level", run.CurrentLevel },
                { "totalLevels", run.TotalLevels },
                { "difficulty", run.Profile.label },
                { "layout", run.LayoutStyle.ToString() },
            });
        }
    }
}