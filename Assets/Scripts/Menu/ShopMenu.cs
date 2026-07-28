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
        // One item kind's offer: the definition it sells and the buttons that buy it and its upgrade.
        // The prices live on the definition; only the scene wiring is authored here.
        [Serializable]
        private class Offer
        {
            public ItemDefinition definition;
            public Button button;
            public Button upgradeButton;
        }

        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text itemLabel; // the highlighted item's price and how many are owned
        [SerializeField] private Offer[] offers;
        [SerializeField] private Sprite unlockedSprite;

        private GameObject _highlighted;

        private void OnEnable()
        {
            foreach (var offer in offers)
            {
                offer.button.onClick.AddListener(() => Buy(offer));
                offer.upgradeButton.onClick.AddListener(() => BuyUpgrade(offer));
                ShowUpgradeSprite(offer); // restore state bought in an earlier session
            }

            ShowBalance();

            if (offers.Length == 0) return;
            _highlighted = offers[0].button.gameObject;
            ShowItem(offers[0]);
        }

        private void OnDisable()
        {
            foreach (var offer in offers)
            {
                offer.button.onClick.RemoveAllListeners();
                offer.upgradeButton.onClick.RemoveAllListeners();
            }
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
            if (CurrencySettings.Balance < offer.definition.price) return;

            CurrencySettings.Balance -= offer.definition.price;
            SaveSystem.Data.ownedItems.Add(offer.definition.kind);
            SaveSystem.Save(); // persist the spend and the new item together
            ShowBalance();
            ShowItem(offer);
        }

        // Buys the kind's upgrade, spending its price, unless it is already bought or the wallet can't afford it.
        // Bought once and kept: every item of the kind is upgraded from then on, this run's and every later one's.
        private void BuyUpgrade(Offer offer)
        {
            if (UpgradeSettings.IsUpgraded(offer.definition.kind)) return;
            if (CurrencySettings.Balance < offer.definition.upgradePrice) return;
            CurrencySettings.Balance -= offer.definition.upgradePrice;
            SaveSystem.Data.upgradedItems.Add(offer.definition.kind);
            SaveSystem.Save();
            ShowBalance();
            ShowUpgradeSprite(offer);
            ShowUpgrade(offer);
        }

        private void ShowBalance() => balanceLabel.text = $"{CurrencySettings.Balance} points";

        private void ShowItem(Offer offer) => itemLabel.text =
            $"{offer.definition.displayName}: {offer.definition.price} points (Owned {OwnedCount(offer.definition.kind)})";

        private void ShowUpgrade(Offer offer)
        {
            itemLabel.text = UpgradeSettings.IsUpgraded(offer.definition.kind)
                ? $"{offer.definition.displayName} Upgrade: Bought"
                : $"{offer.definition.displayName} Upgrade: {offer.definition.upgradePrice} points";
        }

        private static int OwnedCount(ItemKind kind) => SaveSystem.Data.ownedItems.Count(k => k == kind);

        private void ShowUpgradeSprite(Offer offer)
        {
            if (!UpgradeSettings.IsUpgraded(offer.definition.kind)) return;
            if (offer.upgradeButton.TryGetComponent<Image>(out var image) && unlockedSprite)
                image.sprite = unlockedSprite;
        }
    }
}