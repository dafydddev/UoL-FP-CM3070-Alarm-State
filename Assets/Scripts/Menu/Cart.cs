using System.Collections.Generic;
using System.Linq;
using Player;
using Settings;

namespace Menu
{
    // What the player has chosen but not paid for. Checkout buys the lot; leaving the shop drops it.
    public class Cart
    {
        private readonly List<ItemType> _items = new();
        private readonly List<ItemType> _upgrades = new();
        private readonly List<SkinType> _skins = new();

        // The skin checkout equips, chosen with the buy that pays for it.
        private SkinType? _equip;

        public int Total { get; private set; }

        public int CountOf(ItemType type) => _items.Count(k => k == type);

        public bool HasUpgrade(ItemType type) => _upgrades.Contains(type);

        public bool HasSkin(SkinType type) => _skins.Contains(type);

        public void AddItem(ItemDefinition definition)
        {
            _items.Add(definition.type);
            Total += definition.price;
        }

        public void AddUpgrade(ItemDefinition definition)
        {
            _upgrades.Add(definition.type);
            Total += definition.upgradePrice;
        }

        public void AddSkin(SkinDefinition definition)
        {
            _skins.Add(definition.type);
            Total += definition.price;
            _equip = definition.type;
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