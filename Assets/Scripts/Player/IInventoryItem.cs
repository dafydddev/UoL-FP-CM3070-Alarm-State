using Simulation;
using UnityEngine;

namespace Player
{
    // Something the player can carry: picked up into PlayerInventory, then used out of it.
    public interface IInventoryItem
    {
        // Names the item to anything watching the inventory, such as the HUD.
        string ItemId { get; }

        ItemType Type { get; }

        // Runs the item's effect for a user standing on the given cell.
        // Returns false if it cannot act right now, which leaves it in the inventory.
        bool Use(Vector2Int userCell);

        // Whether the item has anything left in it. One that has not been used up stays in the inventory.
        bool IsSpent { get; }

        // What it pays back unused, when a completed run cashes in what the player never spent.
        int CashInValue { get; }

        // Gives the item the world it needs to be usable.
        void Bind(WorldContext world);
    }
}