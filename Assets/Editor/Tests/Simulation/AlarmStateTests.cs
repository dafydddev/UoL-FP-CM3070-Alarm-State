using System;
using System.Collections.Generic;
using NUnit.Framework;
using Simulation;
using UnityEngine;

namespace Editor.Tests.Simulation
{
    public class AlarmStateTests
    {
        private static readonly string[] DividedFacility =
        {
            "#########",
            "#.#.....#",
            "#.#.....#",
            "#.#.....#",
            "#.#.....#",
            "#.......#",
            "#########"
        };

        private static readonly string[] SealedPockets =
        {
            "#####",
            "#.#.#",
            "#.#.#",
            "#.#.#",
            "#.#.#",
            "#####"
        };

        [Test]
        public void TheNearestSwitchIsTheOneWithTheShortestRoute()
        {
            using var grid = new AsciiGrid(DividedFacility);
            var alarm = new AlarmState();
            var closeButWalledOff = new TestSwitch(new Vector2Int(3, 5)); // two cells away, ten to walk
            var furtherButOpen = new TestSwitch(new Vector2Int(3, 1)); // six cells away, six to walk
            alarm.Register(closeButWalledOff);
            alarm.Register(furtherButOpen);
            var nearest = alarm.NearestSwitch(new Vector2Int(1, 5), grid.Pathfinder, null);

            Assert.AreSame(furtherButOpen, nearest);
        }

        [Test]
        public void ASwitchThatCannotBeReachedIsPassedOver()
        {
            using var grid = new AsciiGrid(SealedPockets);
            var alarm = new AlarmState();
            var sealedOff = new TestSwitch(new Vector2Int(3, 1));
            var reachable = new TestSwitch(new Vector2Int(1, 4));
            alarm.Register(sealedOff);
            alarm.Register(reachable);

            Assert.AreSame(reachable, alarm.NearestSwitch(new Vector2Int(1, 1), grid.Pathfinder, null));
        }

        [Test]
        public void NoReachableSwitchMeansNoNearestSwitch()
        {
            using var grid = new AsciiGrid(SealedPockets);
            var alarm = new AlarmState();
            alarm.Register(new TestSwitch(new Vector2Int(3, 1)));

            Assert.IsNull(alarm.NearestSwitch(new Vector2Int(1, 1), grid.Pathfinder, null));
        }

        [Test]
        public void AFacilityWithNoSwitchesHasNoNearestSwitch()
        {
            using var grid = new AsciiGrid(SealedPockets);

            Assert.IsNull(new AlarmState().NearestSwitch(new Vector2Int(1, 1), grid.Pathfinder, null));
        }

        [TestCase(3, 0, 3, true)]
        [TestCase(0, -3, 3, true)]
        [TestCase(3, 3, 3, true)]
        [TestCase(-3, -3, 3, true)]
        [TestCase(4, 0, 3, false)]
        [TestCase(0, 4, 3, false)]
        [TestCase(4, 4, 3, false)]
        public void ASwitchIsInRangeByChebyshevDistance(int offsetX, int offsetY, int cells, bool expected)
        {
            var from = new Vector2Int(10, 10);
            var alarm = new AlarmState();
            alarm.Register(new TestSwitch(from + new Vector2Int(offsetX, offsetY)));

            Assert.AreEqual(expected, alarm.AnySwitchWithin(from, cells));
        }

        [Test]
        public void ANullSwitchIsNeverRegistered()
        {
            var alarm = new AlarmState();

            alarm.Register(null);

            Assert.IsFalse(alarm.AnySwitchWithin(Vector2Int.zero, 5));
        }

        [Test]
        public void RaisingSoundsTheAlarmAndBroadcastsTheContact()
        {
            var alarm = new AlarmState();

            alarm.Raise(new Vector2Int(4, 2), Vector2Int.right);

            Assert.IsTrue(alarm.Active);
            Assert.AreEqual(new Vector2Int(4, 2), alarm.ContactCell);
            Assert.AreEqual(Vector2Int.right, alarm.ContactHeading);
            CollectionAssert.AreEqual(new[] { true }, _broadcasts);
        }

        [Test]
        public void RaisingAnAlreadySoundingAlarmChangesNothing()
        {
            var alarm = new AlarmState();
            alarm.Raise(new Vector2Int(4, 2), Vector2Int.right);

            alarm.Raise(new Vector2Int(9, 9), Vector2Int.up);

            Assert.AreEqual(new Vector2Int(4, 2), alarm.ContactCell);
            Assert.AreEqual(Vector2Int.right, alarm.ContactHeading);
            CollectionAssert.AreEqual(new[] { true }, _broadcasts, "the HUD should hear one raise");
        }

        [Test]
        public void UpdateContactRefreshesTheBroadcastWhileTheAlarmSounds()
        {
            var alarm = new AlarmState();
            alarm.Raise(new Vector2Int(4, 2), Vector2Int.right);

            alarm.UpdateContact(new Vector2Int(6, 3), Vector2Int.up);

            Assert.AreEqual(new Vector2Int(6, 3), alarm.ContactCell);
            Assert.AreEqual(Vector2Int.up, alarm.ContactHeading);
        }

        [Test]
        public void UpdateContactIsIgnoredWhileTheAlarmIsOff()
        {
            var alarm = new AlarmState();

            alarm.UpdateContact(new Vector2Int(6, 3), Vector2Int.up);

            Assert.IsFalse(alarm.Active);
            Assert.AreEqual(Vector2Int.zero, alarm.ContactCell);
        }

        [Test]
        public void DisablingSilencesTheAlarmOnce()
        {
            var alarm = new AlarmState();
            alarm.Raise(new Vector2Int(4, 2), Vector2Int.right);

            alarm.Disable();
            alarm.Disable();

            Assert.IsFalse(alarm.Active);
            CollectionAssert.AreEqual(new[] { true, false }, _broadcasts);
        }

        [Test]
        public void DisablingAnAlarmThatWasNeverRaisedIsSilent()
        {
            new AlarmState().Disable();

            CollectionAssert.IsEmpty(_broadcasts);
        }

        [Test]
        public void AnAlarmCanBeRaisedAgainOnAFreshContact()
        {
            var alarm = new AlarmState();
            alarm.Raise(new Vector2Int(4, 2), Vector2Int.right);
            alarm.Disable();

            alarm.Raise(new Vector2Int(8, 8), Vector2Int.down);

            Assert.IsTrue(alarm.Active);
            Assert.AreEqual(new Vector2Int(8, 8), alarm.ContactCell);
            CollectionAssert.AreEqual(new[] { true, false, true }, _broadcasts);
        }

        [Test]
        public void ResetTellsListenersTheAlarmIsOff()
        {
            AlarmState.Reset();

            CollectionAssert.AreEqual(new[] { false }, _broadcasts);
        }

        private class TestSwitch : IAlarmSwitch
        {
            public TestSwitch(Vector2Int cell) => Cell = cell;
            public Vector2Int Cell { get; }

            public void Activate(Vector2Int contactCell, Vector2Int contactHeading)
            {
            }
        }

        private readonly List<bool> _broadcasts = new();
        private Action<bool> _listener;

        [SetUp]
        public void ListenForBroadcasts()
        {
            _broadcasts.Clear();
            _listener = active => _broadcasts.Add(active);
            AlarmState.ActiveChanged += _listener;
        }

        [TearDown]
        public void StopListening()
        {
            AlarmState.ActiveChanged -= _listener;
        }
    }
}