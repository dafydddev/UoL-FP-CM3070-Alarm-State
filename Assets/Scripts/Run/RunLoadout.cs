using System.Collections.Generic;
using Player;

namespace Run
{
    // The state the next spawned player begins the level with.
    public sealed class RunLoadout
    {
        // Set by the orchestrator: at run start from the player's owned items, then between levels as carry-over.
        // Cleared by the consumer once the spawned player has been filled.
        public static RunLoadout Pending;

        private readonly List<ItemKind> _items = new();

        // One entry per purchased or carried unit, in the order they were added.
        public IReadOnlyList<ItemKind> Items => _items;

        // The hearts to start on, or null to refill to full. Only a carry-over sets this.
        public int? StartingHearts;

        // The kind to put back in the use slot, or null to leave it on the first item granted.
        public ItemKind? Selection;

        public void Add(ItemKind kind) => _items.Add(kind);
    }
}
