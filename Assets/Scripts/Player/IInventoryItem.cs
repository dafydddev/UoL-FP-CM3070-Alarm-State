using UnityEngine;

namespace Player
{
    // Something the player can carry: picked up into PlayerInventory, then used out of it.
    public interface IInventoryItem
    {
        // Names the item to anything watching the inventory, such as the HUD.
        string ItemId { get; }

        ItemKind Kind { get; }

        // Runs the item's effect for a user standing on the given cell.
        // Returns false if it cannot act right now, which leaves it in the inventory.
        bool Use(Vector2Int userCell);
    }
}
