using System.Collections.Generic;
using System.Linq;
using Player;
using Settings;

namespace Menu
{
    // What the player has chosen but not paid for. Checkout buys the lot; leaving the shop drops it.
    public class Cart
    {
        private readonly List<ItemKind> _items = new();
        private readonly List<ItemKind> _upgrades = new();
        private readonly List<SkinKind> _skins = new();

        // The skin checkout equips, chosen with the buy that pays for it.
        private SkinKind? _equip;

        public int Total { get; private set; }

        public int CountOf(ItemKind kind) => _items.Count(k => k == kind);

        public bool HasUpgrade(ItemKind kind) => _upgrades.Contains(kind);

        public bool HasSkin(SkinKind kind) => _skins.Contains(kind);

        public void AddItem(ItemDefinition definition)
        {
            _items.Add(definition.kind);
            Total += definition.price;
        }

        public void AddUpgrade(ItemDefinition definition)
        {
            _upgrades.Add(definition.kind);
            Total += definition.upgradePrice;
        }

        public void AddSkin(SkinDefinition definition)
        {
            _skins.Add(definition.kind);
            Total += definition.price;
            _equip = definition.kind;
        }

        // Persists the spend and everything it bought together.
        public void Commit()
        {
            CurrencySettings.Balance -= Total;
            SaveSystem.Data.ownedItems.AddRange(_items);
            SaveSystem.Data.upgradedItems.AddRange(_upgrades);
            SaveSystem.Data.boughtSkins.AddRange(_skins);
            if (_equip.HasValue) SaveSystem.Data.equippedSkin = _equip.Value;
            SaveSystem.Save();
            Clear();
        }

        public void Clear()
        {
            _items.Clear();
            _upgrades.Clear();
            _skins.Clear();
            _equip = null;
            Total = 0;
        }
    }
}