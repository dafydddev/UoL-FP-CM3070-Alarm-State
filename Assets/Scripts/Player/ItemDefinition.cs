using UnityEngine;

namespace Player
{
    [CreateAssetMenu(menuName = "Items/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public ItemKind kind;

        // Named for the player, since the enum runs uses camel case (e.g. LockPick, not Lock Pick)
        public string displayName;

        [Header("Shop")] [Min(0)] public int price;

        // What buying the upgrade costs. Bought once and kept.
        [Min(0)] public int upgradePrice;

        [Header("World")]
        // Granted to the player for each of the items bought or carried into a level.
        public GameObject prefab;
    }
}
