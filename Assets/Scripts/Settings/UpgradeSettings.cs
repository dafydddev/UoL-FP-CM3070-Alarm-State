using Player;

namespace Settings
{
    // Which item kinds have had their upgrade bought, from the shared save profile.
    // Holds only the unlock: the items themselves stay responsible for what being upgraded is worth to them.
    public static class UpgradeSettings
    {
        // True once the kind's upgrade has been bought, which the shop only lets happen once.
        public static bool IsUpgraded(ItemKind kind) => SaveSystem.Data.upgradedItems.Contains(kind);
    }
}
