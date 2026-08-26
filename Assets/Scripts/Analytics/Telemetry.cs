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
                // Taken only once consent is in place, so nothing can be recorded before then.
                _analytics = AnalyticsService.Instance;
            }
            catch
            {
                // no service, no telemetry; the game plays on regardless
            }
        }

        public static void LevelStarted(RunContext run) => Send(Event("levelStarted", run));

        public static void RunCompleted(RunContext run) => Send(Event("runCompleted", run));

        // Both level endings close the level off the same way, so they report the same timings.
        public static void LevelCompleted(RunContext run, float duration, float timeToFirstAlarm) =>
            LevelEnded("levelCompleted", run, duration, timeToFirstAlarm);

        public static void LevelFailed(RunContext run, float duration, float timeToFirstAlarm) =>
            LevelEnded("levelFailed", run, duration, timeToFirstAlarm);

        // How far into the level this alarm went up. Every raise reports, not just the level's first.
        public static void AlarmRaised(RunContext run, float secondsIntoLevel)
        {
            var e = Event("alarmRaised", run);
            e.Add("secondsIntoLevel", secondsIntoLevel);
            Send(e);
        }

        // timeToFirstAlarm is -1 on a level the player got through unnoticed.
        private static void LevelEnded(string name, RunContext run, float duration, float timeToFirstAlarm)
        {
            var e = Event(name, run);
            e.Add("duration", duration);
            e.Add("timeToFirstAlarm", timeToFirstAlarm);
            Send(e);
        }

        // Every event carries the same snapshot of the run.
        private static CustomEvent Event(string name, RunContext run) => new(name)
        {
            { "level", run.CurrentLevel },
            { "totalLevels", run.TotalLevels },
            { "difficulty", run.DifficultyProfile.label },
            { "layout", run.LayoutStyle.ToString() },
        };

        // Once the SDK has shut down (quit, or leaving play mode) it drops the event on the floor itself.
        private static void Send(CustomEvent e) => _analytics?.RecordEvent(e);
    }
}