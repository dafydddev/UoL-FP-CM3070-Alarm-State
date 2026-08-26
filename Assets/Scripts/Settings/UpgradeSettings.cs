using Player;

namespace Settings
{
    // Which item types have had their upgrade bought from the shared save profile.
    // Holds the unlocked state. The items themselves stay responsible for what being upgraded is worth to them.
    public static class UpgradeSettings
    {
        // True, once the item type's upgrade has been bought, which the shop only lets happen once.
        public static bool IsUpgraded(ItemType type) => SaveSystem.Data.upgradedItems.Contains(type);
    }
}