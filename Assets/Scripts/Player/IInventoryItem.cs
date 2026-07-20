using UnityEngine;

namespace Player
{
    // Something the player can carry: picked up into PlayerInventory, then used out of it.
    // What using it does is the item's own business, so the inventory only has to run the loop.
    public interface IInventoryItem
    {
        // Names the item to anything watching the inventory, such as the HUD.
        string ItemId { get; }

        // Runs the item's effect for a user standing at the given position.
        // Returns false if it cannot act right now, which leaves it in the inventory.
        bool Use(Vector3 userPosition);
    }
}
