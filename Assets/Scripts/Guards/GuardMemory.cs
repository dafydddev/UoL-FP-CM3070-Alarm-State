using Entities;
using UnityEngine;

namespace Guards
{
    // What kind of lead a guard is holding; higher values matter more,
    // so a last-seen player position displaces a noticed distraction but not vice versa.
    public enum LeadKind
    {
        Distraction,
        PlayerLastSeen
    }

    // The guard's working memory:
    // what its senses last established about the player, and the single most important lead worth investigating.
    // Senses write here; the agent reads it to build the planner's world snapshot.
    public class GuardMemory
    {
        public bool SeesPlayer { get; private set; }
        public Vector2Int PlayerCell { get; private set; } // valid while SeesPlayer
        public Vector2Int PlayerHeading { get; private set; } // last cardinal the player was seen moving; zero if unknown

        public bool HasLead { get; private set; }
        public Vector2Int LeadCell { get; private set; }
        private LeadKind LeadKind { get; set; }
        public DistractionItem LeadItem { get; private set; } // set when the lead is a distraction

        public void NotePlayerSeen(Vector2Int cell)
        {
            // Two consecutive sightings reveal which way the player is moving;
            // a fresh sighting starts with an unknown heading until they take a step in view.
            if (!SeesPlayer) PlayerHeading = Vector2Int.zero;
            else if (cell != PlayerCell) PlayerHeading = Heading(PlayerCell, cell);
            SeesPlayer = true;
            PlayerCell = cell;
        }

        // Losing sight turns a position into a lead to investigate. The senses decide where that is:
        // the last-seen cell, or a point projected ahead along PlayerHeading (see GuardSenses).
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