using System;
using System.Collections.Generic;
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
        // True while the alarm is sounding.
        public bool Active { get; private set; }

        // The escape line the alarm points guards at: where the intruder was last seen, and which way they went.
        public Vector2Int ContactCell { get; private set; }
        public Vector2Int ContactHeading { get; private set; }

        // Fires whenever the alarm turns on or off. Static so the HUD can subscribe once.
        public static event Action<bool> ActiveChanged;

        private readonly List<IAlarmSwitch> _switches = new();

        // Switches register themselves at spawn so guards can find the nearest one.
        public void Register(IAlarmSwitch alarmSwitch)
        {
            if (alarmSwitch != null && !_switches.Contains(alarmSwitch)) _switches.Add(alarmSwitch);
        }

        // The switch nearest a cell (Chebyshev, matching how guards measure earshot), or null if none.
        // Skips destroyed switches as a MonoBehaviour lifetime guard.
        public IAlarmSwitch NearestSwitch(Vector2Int from)
        {
            IAlarmSwitch best = null;
            var bestDistance = int.MaxValue;
            foreach (var alarmSwitch in _switches)
            {
                if (alarmSwitch is MonoBehaviour mb && !mb) continue;
                var offset = alarmSwitch.Cell - from;
                var distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = alarmSwitch;
            }

            return best;
        }

        // Raised by a switch once a guard reaches it, with the contact it captured.
        public void Raise(Vector2Int contactCell, Vector2Int contactHeading)
        {
            if (Active) return;
            Active = true;
            ContactCell = contactCell;
            ContactHeading = contactHeading;
            ActiveChanged?.Invoke(true);
        }

        // Refreshes the broadcast to a live position while the alarm sounds.
        public void UpdateContact(Vector2Int contactCell, Vector2Int contactHeading)
        {
            if (!Active) return;
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
