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
    // The shop panel: spends banked currency on the items for the next run, upgrades, and player skins.
    public class ShopMenu : MonoBehaviour
    {
        // One item kind's offer: the definition it sells and the buttons that buy it and its upgrade.
        // The prices live on the definition; only the scene wiring is authored here.
        [Serializable]
        private class ItemOffer
        {
            public ItemDefinition definition;
            public Button button;
            public Button upgradeButton;
        }

        // One skin's offer: the definition it sells and the button that buys it.
        [Serializable]
        private class SkinOffer
        {
            public SkinDefinition definition;
            public Button button;
        }

        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text itemLabel; // the highlighted item's price and how many are owned
        [SerializeField] private ItemOffer[] offers;
        [SerializeField] private SkinOffer[] skinOffers;
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Color balanceErrorColor;
        [SerializeField] private float balanceErrorDuration = 0.5f;

        private GameObject _highlighted;
        private Color _balanceTextColor;
        private float _balanceErrorTimer;

        private void Awake() => _balanceTextColor = balanceLabel.color;

        private void OnEnable()
        {
            foreach (var offer in offers)
            {
                offer.button.onClick.AddListener(() => Buy(offer));
                offer.upgradeButton.onClick.AddListener(() => BuyUpgrade(offer));
                ShowUpgradeSprite(offer);
            }

            foreach (var offer in skinOffers) offer.button.onClick.AddListener(() => BuySkin(offer));

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

            foreach (var offer in skinOffers) offer.button.onClick.RemoveAllListeners();
            balanceLabel.color = _balanceTextColor;
            _balanceErrorTimer = 0f;
        }

        // Refreshes the shared label when the highlight moves to another item or upgrade.
        private void Update()
        {
            FadeBalance();
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _highlighted) return;
            _highlighted = selected;
            var offer = offers.FirstOrDefault(o => o.button.gameObject == selected);
            if (offer != null) ShowItem(offer);
            var upgraded = offers.FirstOrDefault(o => o.upgradeButton.gameObject == selected);
            if (upgraded != null) ShowUpgrade(upgraded);
            var skin = skinOffers.FirstOrDefault(o => o.button.gameObject == selected);
            if (skin != null) ShowSkin(skin);
        }

        // Buys one of the kind, spending its price, unless the wallet can't afford it.
        private void Buy(ItemOffer itemOffer)
        {
            if (CurrencySettings.Balance < itemOffer.definition.price)
            {
                ShowBalanceError();
                return;
            }

            CurrencySettings.Balance -= itemOffer.definition.price;
            SaveSystem.Data.ownedItems.Add(itemOffer.definition.kind);
            SaveSystem.Save(); // persist the spend and the new item together
            ShowBalance();
            ShowItem(itemOffer);
        }

        // Buys the kind's upgrade, spending its price, unless it is already bought or the wallet can't afford it.
        // Bought once and kept: every item of the kind is upgraded from then on, this run's and every later one's.
        private void BuyUpgrade(ItemOffer itemOffer)
        {
            if (UpgradeSettings.IsUpgraded(itemOffer.definition.kind)) return;
            if (CurrencySettings.Balance < itemOffer.definition.upgradePrice)
            {
                ShowBalanceError();
                return;
            }

            CurrencySettings.Balance -= itemOffer.definition.upgradePrice;
            SaveSystem.Data.upgradedItems.Add(itemOffer.definition.kind);
            SaveSystem.Save();
            ShowBalance();
            ShowUpgradeSprite(itemOffer);
            ShowUpgrade(itemOffer);
        }

        // Buys the skin and equips it, unless the wallet can't afford it.
        // Bought once and kept, so pressing it again only equips it.
        private void BuySkin(SkinOffer offer)
        {
            var kind = offer.definition.kind;
            if (!IsBought(kind))
            {
                if (CurrencySettings.Balance < offer.definition.price)
                {
                    ShowBalanceError();
                    return;
                }

                CurrencySettings.Balance -= offer.definition.price;
                SaveSystem.Data.boughtSkins.Add(kind);
            }

            SaveSystem.Data.equippedSkin = kind;
            SaveSystem.Save();
            ShowBalance();
            ShowSkin(offer);
        }

        private void ShowBalance() => balanceLabel.text = $"{CurrencySettings.Balance} points";

        private void ShowItem(ItemOffer itemOffer)
        {
            itemLabel.text =
                $"{itemOffer.definition.displayName}: {itemOffer.definition.price} points (Owned {OwnedCount(itemOffer.definition.kind)})";
            if (itemOffer.definition.price > CurrencySettings.Balance) itemLabel.text += " Insufficient funds";
        }

        private void ShowUpgrade(ItemOffer itemOffer)
        {
            itemLabel.text = UpgradeSettings.IsUpgraded(itemOffer.definition.kind)
                ? $"{itemOffer.definition.displayName} Upgrade: Bought"
                : $"{itemOffer.definition.displayName} Upgrade: {itemOffer.definition.upgradePrice} points";
            if (itemOffer.definition.upgradePrice > CurrencySettings.Balance) itemLabel.text += " Insufficient funds";
        }

        private void ShowSkin(SkinOffer offer)
        {
            var definition = offer.definition;
            if (SaveSystem.Data.equippedSkin == definition.kind) itemLabel.text = $"{definition.displayName}: Equipped";
            else if (IsBought(definition.kind)) itemLabel.text = $"{definition.displayName}: Bought";
            else itemLabel.text = $"{definition.displayName}: {definition.price} points";
            if (definition.price > CurrencySettings.Balance) itemLabel.text += " Insufficient funds";
        }

        private static int OwnedCount(ItemKind kind) => SaveSystem.Data.ownedItems.Count(k => k == kind);

        // The skin the player starts in is theirs without buying.
        private static bool IsBought(SkinKind kind) =>
            kind == SkinKind.Default || SaveSystem.Data.boughtSkins.Contains(kind);

        private void ShowUpgradeSprite(ItemOffer itemOffer)
        {
            if (!UpgradeSettings.IsUpgraded(itemOffer.definition.kind)) return;
            if (itemOffer.upgradeButton.TryGetComponent<Image>(out var image) && unlockedSprite)
                image.sprite = unlockedSprite;
        }

        // Flashes the balance to signal that the wallet cannot afford the press.
        private void ShowBalanceError()
        {
            _balanceErrorTimer = balanceErrorDuration;
            balanceLabel.color = balanceErrorColor;
        }

        // Eases the balance back to its authored colour over the remainder of the flash.
        private void FadeBalance()
        {
            if (_balanceErrorTimer <= 0f) return;
            _balanceErrorTimer = Mathf.Max(0f, _balanceErrorTimer - Time.unscaledDeltaTime);
            balanceLabel.color = Color.Lerp(_balanceTextColor, balanceErrorColor,
                _balanceErrorTimer / balanceErrorDuration);
        }
    }
}