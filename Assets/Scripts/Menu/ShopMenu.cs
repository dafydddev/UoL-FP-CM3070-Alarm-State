using System;
using System.Linq;
using Player;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // The shop panel: spends banked currency on the items for the next run and offers to upgrade them.
    public class ShopMenu : MonoBehaviour
    {
        // One item kind's offer: its authored price and the button that buys it, alongside its upgrade.
        [Serializable]
        private class Offer
        {
            public ItemKind kind;
            public int price;
            public Button button;
            public int upgradePrice;
            public Button upgradeButton;
        }

        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text itemLabel; // the highlighted item's price and how many are owned
        [SerializeField] private Offer[] offers;
        [SerializeField] private Sprite unlockedSprite;

        private GameObject _highlighted;

        private void Start()
        {
            foreach (var offer in offers)
            {
                offer.button.onClick.AddListener(() => Buy(offer));
                offer.upgradeButton.onClick.AddListener(() => BuyUpgrade(offer));
                ShowUpgradeSprite(offer); // restore state bought in an earlier session
            }

            ShowBalance();
        }

        // Refreshes the shared label when the highlight moves to another item or upgrade.
        private void Update()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _highlighted) return;
            _highlighted = selected;
            var offer = offers.FirstOrDefault(o => o.button.gameObject == selected);
            if (offer != null) ShowItem(offer);
            var upgraded = offers.FirstOrDefault(o => o.upgradeButton.gameObject == selected);
            if (upgraded != null) ShowUpgrade(upgraded);
        }

        // Buys one of the kind, spending its price, unless the wallet can't afford it.
        private void Buy(Offer offer)
        {
            if (CurrencySettings.Balance < offer.price) return;

            CurrencySettings.Balance -= offer.price;
            SaveSystem.Data.ownedItems.Add(offer.kind);
            SaveSystem.Save(); // persist the spend and the new item together
            ShowBalance();
            ShowItem(offer);
        }

        // Buys the kind's upgrade, spending its price, unless it is already bought or the wallet can't afford it.
        // Bought once and kept: every item of the kind is upgraded from then on, this run's and every later one's.
        private void BuyUpgrade(Offer offer)
        {
            if (UpgradeSettings.IsUpgraded(offer.kind)) return;
            if (CurrencySettings.Balance < offer.upgradePrice) return;
            CurrencySettings.Balance -= offer.upgradePrice;
            SaveSystem.Data.upgradedItems.Add(offer.kind);
            SaveSystem.Save();
            ShowBalance();
            ShowUpgradeSprite(offer);
            ShowUpgrade(offer);
        }


        private void ShowBalance() => balanceLabel.text = $"{CurrencySettings.Balance} points";

        private void ShowItem(Offer offer) => itemLabel.text =
            $"{NameOf(offer.kind)}: {offer.price} points (Owned {OwnedCount(offer.kind)})";

        private void ShowUpgrade(Offer offer)
        {
            itemLabel.text = UpgradeSettings.IsUpgraded(offer.kind)
                ? $"{NameOf(offer.kind)} Upgrade: Bought"
                : $"{NameOf(offer.kind)} Upgrade: {offer.upgradePrice} points";
        }

        private static int OwnedCount(ItemKind kind) => SaveSystem.Data.ownedItems.Count(k => k == kind);

        // The spaced name for a kind, since the enum runs the words together.
        private static string NameOf(ItemKind kind) => kind switch
        {
            ItemKind.LockPick => "Lock Pick",
            ItemKind.HealthPack => "Health Pack",
            _ => kind.ToString(),
        };
        
        private void ShowUpgradeSprite(Offer offer)
        {
            if (!UpgradeSettings.IsUpgraded(offer.kind)) return;
            if (offer.upgradeButton.TryGetComponent<Image>(out var image) && unlockedSprite) image.sprite = unlockedSprite;
        }
    }
}