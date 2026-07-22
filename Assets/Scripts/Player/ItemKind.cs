namespace Player
{
    // The kinds of item the player can carry: one slot per kind on the inventory screen, and one shown in the use slot.
    // The kind, not the item id, is what stacks and slots, so a run's many distractions still share a single slot.
    public enum ItemKind
    {
        Distraction,
        Disguise,
        LockPick,
        HealthPack
    }
}
