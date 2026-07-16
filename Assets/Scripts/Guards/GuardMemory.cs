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

    // The guard's working memory: what its senses last established about the player,
    // the single most important lead worth investigating, and short-lived cooldowns.
    // Senses write here; the agent reads it to build the planner's world snapshot.
    public class GuardMemory
    {
        public bool SeesPlayer { get; private set; }
        public Vector2Int PlayerCell { get; private set; } // valid while SeesPlayer

        public bool HasLead { get; private set; }
        public Vector2Int LeadCell { get; private set; }
        private LeadKind LeadKind { get; set; }
        public DistractionItem LeadItem { get; private set; } // set when the lead is a distraction

        // True for a short while after an arrest, so the catch goal doesn't refire every tick.
        public bool RecentlyCaughtPlayer => _arrestCooldown > 0;
        private int _arrestCooldown;

        public void NotePlayerSeen(Vector2Int cell)
        {
            SeesPlayer = true;
            PlayerCell = cell;
        }

        // Losing sight turns the last known position into a lead to investigate.
        public void NotePlayerLost()
        {
            if (!SeesPlayer) return;
            SeesPlayer = false;
            OfferLead(PlayerCell, LeadKind.PlayerLastSeen);
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

        public void BeginArrestCooldown(int ticks) => _arrestCooldown = ticks;

        // Called once per agent tick to age time-based memory.
        public void TickCooldowns()
        {
            if (_arrestCooldown > 0) _arrestCooldown--;
        }
    }
}
