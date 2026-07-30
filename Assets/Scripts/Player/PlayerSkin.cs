using System.Collections.Generic;
using Settings;
using UnityEngine;

namespace Player
{
    // The skin the player appears in, as bought and equipped in the shop.
    public class PlayerSkin : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;

        // The looks the shop sells. Each is keyed by the kind it carries, so the order they sit in doesn't matter.
        [SerializeField] private SkinDefinition[] skins;

        private Dictionary<SkinKind, SkinDefinition> _lookup;

        // Built on first use rather than in Awake, so being asked before (e.g. by PlayerDisguise) is safe.
        private Dictionary<SkinKind, SkinDefinition> Lookup
        {
            get
            {
                if (_lookup != null) return _lookup;
                _lookup = new Dictionary<SkinKind, SkinDefinition>();
                foreach (var skin in skins)
                {
                    if (skin) _lookup[skin.kind] = skin;
                }

                return _lookup;
            }
        }

        // Null when the equipped skin has no definition, leaving whoever asked on the sprite set in the inspector.
        public Sprite Equipped => Lookup.TryGetValue(SaveSystem.Data.equippedSkin, out var skin) ? skin.sprite : null;

        private void Awake()
        {
            var equipped = Equipped;
            if (sprite && equipped) sprite.sprite = equipped;
        }
    }
}