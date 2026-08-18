using System.Collections.Generic;
using Entities.Items;
using Guards;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests.Guards
{
    public class GuardMemoryTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void DestroySpawnedItems()
        {
            foreach (var spawned in _spawned) Object.DestroyImmediate(spawned);
            _spawned.Clear();
        }

        private DistractionItem NewDistraction()
        {
            var host = new GameObject("distraction");
            _spawned.Add(host);
            return host.AddComponent<DistractionItem>();
        }

        [Test]
        public void AFreshSightingHasNoKnownHeading()
        {
            var memory = new GuardMemory();

            memory.NotePlayerSeen(new Vector2Int(4, 7));

            Assert.IsTrue(memory.SeesPlayer);
            Assert.AreEqual(new Vector2Int(4, 7), memory.PlayerCell);
            Assert.AreEqual(Vector2Int.zero, memory.PlayerHeading);
        }

        [TestCase(1, 0, 1, 0)]
        [TestCase(-1, 0, -1, 0)]
        [TestCase(0, 1, 0, 1)]
        [TestCase(0, -1, 0, -1)]
        [TestCase(1, 1, 1, 0)]
        [TestCase(-1, -1, -1, 0)]
        [TestCase(1, 2, 0, 1)]
        [TestCase(2, -1, 1, 0)]
        public void ASecondSightingRevealsTheHeading(int stepX, int stepY, int headingX, int headingY)
        {
            var memory = new GuardMemory();
            var start = new Vector2Int(5, 5);

            memory.NotePlayerSeen(start);
            memory.NotePlayerSeen(start + new Vector2Int(stepX, stepY));

            Assert.AreEqual(new Vector2Int(headingX, headingY), memory.PlayerHeading);
        }

        [Test]
        public void SeeingThePlayerStandStillKeepsTheHeading()
        {
            var memory = new GuardMemory();
            memory.NotePlayerSeen(new Vector2Int(5, 5));
            memory.NotePlayerSeen(new Vector2Int(6, 5));

            memory.NotePlayerSeen(new Vector2Int(6, 5));

            Assert.AreEqual(new Vector2Int(1, 0), memory.PlayerHeading);
        }

        [Test]
        public void LosingSightLeavesAPlayerTrailAtTheGivenCell()
        {
            var memory = new GuardMemory();
            memory.NotePlayerSeen(new Vector2Int(5, 5));

            memory.NotePlayerLost(new Vector2Int(8, 5));

            Assert.IsFalse(memory.SeesPlayer);
            Assert.IsTrue(memory.LeadIsPlayerTrail);
            Assert.AreEqual(new Vector2Int(8, 5), memory.LeadCell);
        }

        [Test]
        public void LosingSightIsIgnoredWhenThePlayerWasNeverInView()
        {
            var memory = new GuardMemory();

            memory.NotePlayerLost(new Vector2Int(8, 5));

            Assert.IsFalse(memory.HasLead);
        }

        [Test]
        public void ASecondLossDoesNotMoveTheTrail()
        {
            var memory = new GuardMemory();
            memory.NotePlayerSeen(new Vector2Int(5, 5));
            memory.NotePlayerLost(new Vector2Int(8, 5));

            memory.NotePlayerLost(new Vector2Int(0, 0));

            Assert.AreEqual(new Vector2Int(8, 5), memory.LeadCell);
        }

        [Test]
        public void OnlyAnEqualOrMoreImportantLeadDisplacesTheOneHeld([Values] LeadKind held, [Values] LeadKind offered)
        {
            var memory = new GuardMemory();
            var standing = new Vector2Int(1, 1);
            var fresh = new Vector2Int(9, 9);

            memory.OfferLead(standing, held);
            memory.OfferLead(fresh, offered);

            Assert.AreEqual(offered >= held ? fresh : standing, memory.LeadCell);
        }

        [Test]
        public void ADistractionDoesNotPullAGuardOffAPlayerTrail()
        {
            var memory = new GuardMemory();
            memory.NotePlayerSeen(new Vector2Int(3, 3));
            memory.NotePlayerLost(new Vector2Int(3, 3));

            memory.OfferLead(new Vector2Int(10, 10), LeadKind.Distraction, NewDistraction());

            Assert.IsTrue(memory.LeadIsPlayerTrail);
            Assert.AreEqual(new Vector2Int(3, 3), memory.LeadCell);
        }

        [Test]
        public void TheAlarmDisplacesEveryOtherLead(
            [Values(LeadKind.Distraction, LeadKind.PlayerLastSeen)]
            LeadKind held)
        {
            var memory = new GuardMemory();
            memory.OfferLead(new Vector2Int(2, 2), held);

            memory.OfferLead(new Vector2Int(7, 7), LeadKind.Alarm);

            Assert.AreEqual(new Vector2Int(7, 7), memory.LeadCell);
            Assert.IsFalse(memory.LeadIsPlayerTrail);
        }

        [Test]
        public void ADistractionLeadCarriesTheItemThatCausedIt()
        {
            var memory = new GuardMemory();
            var item = NewDistraction();

            memory.OfferLead(new Vector2Int(4, 4), LeadKind.Distraction, item);

            Assert.AreSame(item, memory.LeadItem);
        }

        [Test]
        public void ATrailSupersedingADistractionDropsItsItem()
        {
            var memory = new GuardMemory();
            memory.OfferLead(new Vector2Int(4, 4), LeadKind.Distraction, NewDistraction());

            memory.OfferLead(new Vector2Int(8, 1), LeadKind.PlayerLastSeen);

            Assert.IsNull(memory.LeadItem);
        }

        [Test]
        public void ClearLeadDropsTheItemToo()
        {
            var memory = new GuardMemory();
            memory.OfferLead(new Vector2Int(4, 4), LeadKind.Distraction, NewDistraction());

            memory.ClearLead();

            Assert.IsFalse(memory.HasLead);
            Assert.IsNull(memory.LeadItem);
        }

        [Test]
        public void TheAlarmIsOnlyWantedOnceTheTrailHasRunOut()
        {
            var memory = new GuardMemory();
            Assert.IsFalse(memory.WantsToRaiseAlarm);

            memory.MarkTrailLost();
            Assert.IsTrue(memory.WantsToRaiseAlarm);

            memory.MarkAlarmSought();
            Assert.IsFalse(memory.WantsToRaiseAlarm, "the guard should not set off for a switch twice");
        }

        [Test]
        public void AFreshSightingReArmsTheAlarm()
        {
            var memory = new GuardMemory();
            memory.MarkTrailLost();
            memory.MarkAlarmSought();

            memory.NotePlayerSeen(new Vector2Int(3, 4));
            Assert.IsFalse(memory.WantsToRaiseAlarm, "a guard back in pursuit has no lost trail");

            memory.NotePlayerLost(new Vector2Int(3, 4));
            memory.MarkTrailLost();
            Assert.IsTrue(memory.WantsToRaiseAlarm);
        }

        [Test]
        public void AContactIsAnsweredOnlyOnce()
        {
            var memory = new GuardMemory();
            var contact = new Vector2Int(6, 2);
            Assert.IsFalse(memory.HasAnsweredAlarm(1, contact));

            memory.MarkAlarmAnswered(1, contact);

            Assert.IsTrue(memory.HasAnsweredAlarm(1, contact));
        }

        [Test]
        public void AMovedContactHasNotBeenAnswered()
        {
            var memory = new GuardMemory();
            memory.MarkAlarmAnswered(1, new Vector2Int(6, 2));

            Assert.IsFalse(memory.HasAnsweredAlarm(1, new Vector2Int(6, 3)));
        }

        [Test]
        public void AnAlarmRaisedAgainAtTheSameContactIsAnsweredAfresh()
        {
            var memory = new GuardMemory();
            var contact = new Vector2Int(6, 2);
            memory.MarkAlarmAnswered(1, contact);

            Assert.IsFalse(memory.HasAnsweredAlarm(2, contact),
                "silencing the alarm makes covered ground worth sweeping again");
        }

        [Test]
        public void AnAlarmAlreadySoundingSettlesTheUrgeToRaiseOne()
        {
            var memory = new GuardMemory();
            memory.MarkTrailLost();

            memory.MarkAlarmSought(); // HearAlarm, once the alarm sounds

            Assert.IsFalse(memory.WantsToRaiseAlarm, "the alarm already says what this guard set out to report");
        }
    }
}