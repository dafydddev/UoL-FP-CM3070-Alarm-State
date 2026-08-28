using Entities.Items;
using UnityEngine;

namespace Guards
{
    // What kind of lead a guard is holding. Higher values matter more.
    // A last-seen player position displaces a noticed distraction but not vice versa, and a sounding alarm displaces both.
    public enum LeadKind
    {
        Distraction,
        PlayerLastSeen,
        Alarm
    }

    // The guard's working memory.
    // What its senses last established about the player, and the single most important lead worth investigating.
    public class GuardMemory
    {
        public bool SeesPlayer { get; private set; }
        public Vector2Int PlayerCell { get; private set; } // valid while SeesPlayer
        public Vector2Int PlayerHeading { get; private set; } // last cardinal the player was seen moving; zero if unknown

        public bool HasLead { get; private set; }
        public Vector2Int LeadCell { get; private set; }
        private LeadKind LeadKind { get; set; }
        public DistractionItem LeadItem { get; private set; } // set when the lead is a distraction

        // The current lead is a player trail the guard is following for itself, not a distraction.
        public bool LeadIsPlayerTrail => HasLead && LeadKind == LeadKind.PlayerLastSeen;

        // The alarm is raised only after a first-hand trail has run out. A fresh sighting re-arms.
        private bool _trailLost;
        private bool _alarmSought;
        public bool WantsToRaiseAlarm => _trailLost && !_alarmSought;
        public void MarkTrailLost() => _trailLost = true;
        public void MarkAlarmSought() => _alarmSought = true;

        // A guard answers a given contact once: it sweeps its stretch, then stops being pulled and returns to patrol.
        // A contact that moves (a fresh sighting) re-arms it, as does the alarm being silenced and raised again.
        private int _answeredSounding; // soundings number from 1, so zero is none answered
        private Vector2Int _answeredContact;

        public bool HasAnsweredAlarm(int sounding, Vector2Int contact) =>
            _answeredSounding == sounding && _answeredContact == contact;

        public void MarkAlarmAnswered(int sounding, Vector2Int contact)
        {
            _answeredSounding = sounding;
            _answeredContact = contact;
        }

        public void NotePlayerSeen(Vector2Int cell)
        {
            // Two consecutive sightings reveal which way the player is moving.
            // A fresh sighting starts with an unknown heading until they take a step in view.
            if (!SeesPlayer)
            {
                PlayerHeading = Vector2Int.zero;
                _trailLost = false;   // a fresh sighting restarts the trail
                _alarmSought = false; // and re-arms the alarm
            }
            else if (cell != PlayerCell) PlayerHeading = Heading(PlayerCell, cell);
            SeesPlayer = true;
            PlayerCell = cell;
        }

        // Losing sight turns a position into a lead to investigate.
        // The senses decide where that is the last-seen cell, or a point projected ahead along PlayerHeading.
        public void NotePlayerLost(Vector2Int leadCell)
        {
            if (!SeesPlayer) return;
            SeesPlayer = false;
            OfferLead(leadCell, LeadKind.PlayerLastSeen);
        }

        // The dominant cardinal direction from one cell to another (zero if they coincide).
        private static Vector2Int Heading(Vector2Int from, Vector2Int to)
        {
            var d = to - from;
            if (d == Vector2Int.zero) return Vector2Int.zero;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return new Vector2Int(d.x > 0 ? 1 : -1, 0);
            return new Vector2Int(0, d.y > 0 ? 1 : -1);
        }

        // Takes the lead unless a more important one is already held.
        // An equal kind is replaced: fresher information wins.
        public void OfferLead(Vector2Int cell, LeadKind kind, DistractionItem item = null)
        {
            if (HasLead && kind < LeadKind) return;
            HasLead = true;
            LeadCell = cell;
            LeadKind = kind;
            LeadItem = item;
        }

        public void ClearLead()
        {
            HasLead = false;
            LeadItem = null;
        }
    }
}