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

        [Min(0)] public int upgradePrice;

        [Header("World")] public GameObject prefab;
    }
}