namespace Player
{
    // The types of item the player can carry: one slot per type on the inventory screen, and one shown in the use slot.
    // The type, not the item id, is what stacks and slots, so a run's many distractions still share a single slot.
    public enum ItemType
    {
        Distraction,
        Disguise,
        LockPick,
        HealthPack
    }
}
