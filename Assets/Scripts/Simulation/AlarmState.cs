using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace Simulation
{
    // A switch a guard trips and the player disables.
    public interface IAlarmSwitch
    {
        Vector2Int Cell { get; }
        void Activate(Vector2Int contactCell, Vector2Int contactHeading);
    }

    // Per-level alarm state, rebuilt with the WorldContext each level like MissionProgress.
    // A guard raises it at a switch, broadcasting the player's last-seen cell and heading direction.
    public sealed class AlarmState
    {
        // How far the sirens carry, in cells.
        private const int BroadcastRadiusCells = 25;

        // True while the alarm is sounding.
        public bool Active { get; private set; }

        // The escape line the alarm points guards at. Where the intruder was last seen, and which way they went.
        public Vector2Int ContactCell { get; private set; }
        public Vector2Int ContactHeading { get; private set; }

        // Which sounding this is; each raise is new. Guards remember which one they answered.
        public int SoundingId { get; private set; }

        // True once a guard has refreshed the contact from a live sighting.
        public bool ContactRelayed { get; private set; }

        // Fires when the alarm turns on or off. Static so the HUD can subscribe once.
        public static event Action<bool> ActiveChanged;

        private readonly List<IAlarmSwitch> _switches = new();

        // Scratch list for ranking switches, reused so weighing them up doesn't allocate on every guard's tick.
        private readonly List<(IAlarmSwitch alarmSwitch, int lowerBound)> _ranked = new();

        // Switches register themselves at spawn so guards can find the nearest one.
        public void Register(IAlarmSwitch alarmSwitch)
        {
            if (alarmSwitch != null && !_switches.Contains(alarmSwitch)) _switches.Add(alarmSwitch);
        }

        // Whether a sounding alarm reaches a cell.
        public bool AudibleAt(Vector2Int cell) => Active && AnySwitchWithin(cell, BroadcastRadiusCells);

        // Whether a switch stands within a straight-line range of a cell, for range checks like earshot.
        // Skips destroyed switches as a MonoBehaviour lifetime guard.
        public bool AnySwitchWithin(Vector2Int from, int cells)
        {
            foreach (var alarmSwitch in _switches)
            {
                if (alarmSwitch is MonoBehaviour mb && !mb) continue;
                var offset = alarmSwitch.Cell - from;
                if (Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) <= cells) return true;
            }

            return false;
        }

        // The switch this mover reaches by the shortest walk, or null if it can reach none.
        // Distance is route length, so walls and doors this mover can't open count.
        public IAlarmSwitch NearestSwitch(Vector2Int from, AStarPathfinder pathfinder, Actor mover)
        {
            _ranked.Clear();
            foreach (var alarmSwitch in _switches)
            {
                if (alarmSwitch is MonoBehaviour mb && !mb) continue;
                var offset = alarmSwitch.Cell - from;
                _ranked.Add((alarmSwitch, Mathf.Abs(offset.x) + Mathf.Abs(offset.y)));
            }

            // Find the best round to the nearest alarm switch.
            _ranked.Sort((a, b) => a.lowerBound.CompareTo(b.lowerBound));

            IAlarmSwitch best = null;
            var bestSteps = int.MaxValue;
            foreach (var (alarmSwitch, lowerBound) in _ranked)
            {
                if (lowerBound >= bestSteps) break;
                var route = pathfinder.FindPath(from, alarmSwitch.Cell, mover);
                if (route == null) continue;
                var steps = route.Count - 1; // the route includes the cell the mover stands on
                if (steps >= bestSteps) continue;
                bestSteps = steps;
                best = alarmSwitch;
            }

            return best;
        }

        // Raised by a switch once a guard reaches it, with the contact it captured.
        public void Raise(Vector2Int contactCell, Vector2Int contactHeading)
        {
            if (Active) return;
            Active = true;
            SoundingId++;
            ContactRelayed = false;
            ContactCell = contactCell;
            ContactHeading = contactHeading;
            ActiveChanged?.Invoke(true);
        }

        // Refreshes the broadcast to a live position while the alarm sounds, and marks it relayed.
        public void UpdateContact(Vector2Int contactCell, Vector2Int contactHeading)
        {
            if (!Active) return;
            ContactRelayed = true;
            ContactCell = contactCell;
            ContactHeading = contactHeading;
        }

        // Turned off by the player using a switch.
        public void Disable()
        {
            if (!Active) return;
            Active = false;
            ActiveChanged?.Invoke(false);
        }

        // Called when a new level is built so anything latched on the static event clears.
        public static void Reset() => ActiveChanged?.Invoke(false);
    }
}
