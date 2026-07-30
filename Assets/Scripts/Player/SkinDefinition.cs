using UnityEngine;

namespace Player
{
    [CreateAssetMenu(menuName = "Player/Skin Definition")]
    public class SkinDefinition : ScriptableObject
    {
        public SkinKind kind;

        public string displayName;

        [Header("Shop")] [Min(0)] public int price;

        [Header("World")] public Sprite sprite;
    }
}