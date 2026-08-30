using NUnit.Framework;
using Run;

namespace Editor.Tests.Progression
{
    // The window of recent levels that the room graph's adaptive pass reads.
    public class RunPerformanceTests
    {
        private const int MaxHearts = 3;

        [Test]
        public void StandingIsLevelUntilTheWindowFills()
        {
            var performance = new RunPerformance();
            for (var i = 0; i < RunPerformance.Window - 1; i++)
            {
                performance.RecordLevel(1, MaxHearts, 2);
            }

            Assert.That(performance.Standing, Is.EqualTo(0f), "standing should be 0 until the window is full");
            Assert.That(performance.CanInject(1), Is.False, "no room should be added before the window fills");
        }

        [Test]
        public void UntouchedHeartsAndNoAlarmsReadsAsThriving()
        {
            var performance = Filled(MaxHearts, 0);

            Assert.That(performance.Standing, Is.EqualTo(1f));
            Assert.That(performance.CanInject(RunPerformance.Window), Is.True);
        }

        // The score measures the bar above the first heart, so one heart left reads as nothing left.
        [Test]
        public void ALastHeartReadsAsStruggling()
        {
            Assert.That(Filled(1, 0).Standing, Is.EqualTo(-1f));
        }

        // Alarms pull the score down even when no hearts were lost.
        [Test]
        public void AlarmsPullAThrivingRunBackDown()
        {
            Assert.That(Filled(MaxHearts, 2).Standing, Is.LessThanOrEqualTo(0f));
        }

        [Test]
        public void OneBadLevelInAGoodWindowStaysNearLevel()
        {
            var performance = new RunPerformance();
            performance.RecordLevel(MaxHearts, MaxHearts, 0);
            performance.RecordLevel(MaxHearts, MaxHearts, 0);
            performance.RecordLevel(1, MaxHearts, 0);

            Assert.That(performance.Standing, Is.EqualTo(1f / 3f).Within(0.001f));
        }

        // Only the last few levels count, so older ones stop affecting the standing.
        [Test]
        public void FormOlderThanTheWindowIsForgotten()
        {
            var performance = new RunPerformance();
            for (var i = 0; i < RunPerformance.Window; i++)
            {
                performance.RecordLevel(MaxHearts, MaxHearts, 0);
            }

            for (var i = 0; i < RunPerformance.Window; i++)
            {
                performance.RecordLevel(1, MaxHearts, 0);
            }

            Assert.That(performance.Standing, Is.EqualTo(-1f));
        }

        // The cooldown counts from a room that was actually added, so a full window can inject at any level.
        [Test]
        public void AFullWindowCanInjectBeforeAnyRoomHasBeenAdded([Values(0, 1, 2)] int level)
        {
            Assert.That(Filled(1, 0).CanInject(level), Is.True);
        }

        [Test]
        public void TheCooldownHoldsOffTheNextInjection()
        {
            var performance = Filled(1, 0);
            performance.RecordInjection(10);

            Assert.That(performance.CanInject(10 + RunPerformance.Cooldown), Is.False, "still inside the cooldown");
            Assert.That(performance.CanInject(10 + RunPerformance.Cooldown + 1), Is.True, "the cooldown has passed");
        }

        // Rebuilding the level a room was added at must give the same answer.
        [Test]
        public void TheInjectedLevelItselfStillPasses()
        {
            var performance = Filled(1, 0);
            performance.RecordInjection(10);

            Assert.That(performance.CanInject(10), Is.True);
        }

        // A full window of identical levels.
        private static RunPerformance Filled(int hearts, int alarms)
        {
            var performance = new RunPerformance();
            for (var i = 0; i < RunPerformance.Window; i++)
            {
                performance.RecordLevel(hearts, MaxHearts, alarms);
            }

            return performance;
        }
    }
}