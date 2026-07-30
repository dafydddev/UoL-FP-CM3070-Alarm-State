using Settings;
using UnityEngine;

namespace Player
{
    // The skin the player appears in, as bought and equipped in the shop.
    public class PlayerSkin : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private SkinDefinition @default;
        [SerializeField] private SkinDefinition soldier;
        [SerializeField] private SkinDefinition king;
        [SerializeField] private SkinDefinition unknown;

        // Null when nothing is equipped, leaving whoever asked on the sprite set in the inspector.
        public Sprite Equipped
        {
            get
            {
                var equipped = SaveSystem.Data.equippedSkin switch
                {
                    SkinKind.Default => @default,
                    SkinKind.Solider => soldier,
                    SkinKind.King => king,
                    SkinKind.Unknown => unknown,
                    _ => null
                };

                return equipped ? equipped.sprite : null;
            }
        }

        private void Awake()
        {
            var equipped = Equipped;
            if (sprite && equipped) sprite.sprite = equipped;
        }
    }
}
