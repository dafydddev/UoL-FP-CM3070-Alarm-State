using System;
using System.Linq;
using Audio;
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
        // One item type's offer: the definition it sells and the buttons that buy it and its upgrade.
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
        [SerializeField] private Button resetCartButton;
        [SerializeField] private Button checkoutButton;
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private UISfxController uiSfxController;
        [SerializeField] private Color balanceErrorColor;
        [SerializeField] private float balanceErrorDuration = 0.5f;

        private readonly Cart _cart = new();
        private GameObject _highlighted;
        private Color _balanceTextColor;
        private float _balanceErrorTimer;

        // The offer the label is showing, which checkout and reset take the highlight away from.
        private GameObject _shown;

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

            checkoutButton.onClick.AddListener(Checkout);
            resetCartButton.onClick.AddListener(ResetCart);

            ShowBalance();
            ShowCartButtons();

            if (offers.Length == 0) return;
            _highlighted = offers[0].button.gameObject;
            _shown = _highlighted;
            ShowItem(offers[0]);
        }

        private void OnDisable()
        {
            foreach (var offer in offers)
            {
                offer.button.onClick.RemoveAllListeners();
                offer.upgradeButton.onClick.RemoveAllListeners();
            }

            foreach (var offer in skinOffers)
            {
                offer.button.onClick.RemoveAllListeners();
            }

            checkoutButton.onClick.RemoveAllListeners();
            resetCartButton.onClick.RemoveAllListeners();
            balanceLabel.color = _balanceTextColor;
            _balanceErrorTimer = 0f;
            _cart.Clear(); // leaving the shop drops what was never paid for
        }

        // Refreshes the shared label when the highlight moves to another item or upgrade.
        private void Update()
        {
            FadeBalance();
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _highlighted) return;
            _highlighted = selected;
            // Checkout and reset are not offers, so the label stays on the one it was showing.
            if (ShowOffer(selected)) _shown = selected;
        }

        // Shows the offer the object is a button for. False if it is not one of them.
        private bool ShowOffer(GameObject selected)
        {
            var offer = offers.FirstOrDefault(o => o.button.gameObject == selected);
            if (offer != null)
            {
                ShowItem(offer);
                return true;
            }

            var upgraded = offers.FirstOrDefault(o => o.upgradeButton.gameObject == selected);
            if (upgraded != null)
            {
                ShowUpgrade(upgraded);
                return true;
            }

            var skin = skinOffers.FirstOrDefault(o => o.button.gameObject == selected);
            if (skin == null) return false;
            ShowSkin(skin);
            return true;
        }

        // Carts one of the item types, unless the wallet can't afford it on top of the cart.
        private void Buy(ItemOffer itemOffer)
        {
            if (Remaining < itemOffer.definition.price)
            {
                ShowBalanceError();
                return;
            }

            _cart.AddItem(itemOffer.definition);
            ShowBalance();
            ShowCartButtons();
            ShowItem(itemOffer);
        }

        // Carts the item type's upgrade, unless it is already bought or carted, or the wallet can't afford it.
        // Bought once and kept: every item of the type is upgraded from then on, this run's and every later one's.
        private void BuyUpgrade(ItemOffer itemOffer)
        {
            var itemType = itemOffer.definition.type;
            if (UpgradeSettings.IsUpgraded(itemType) || _cart.HasUpgrade(itemType)) return;
            if (Remaining < itemOffer.definition.upgradePrice)
            {
                ShowBalanceError();
                return;
            }

            _cart.AddUpgrade(itemOffer.definition);
            ShowBalance();
            ShowCartButtons();
            ShowUpgradeSprite(itemOffer);
            ShowUpgrade(itemOffer);
        }

        // Carts the skin and equips it, unless the wallet can't afford it.
        // Bought once and kept, so pressing an owned one only equips it.
        private void BuySkin(SkinOffer offer)
        {
            var skinType = offer.definition.type;
            if (IsBought(skinType))
            {
                SaveSystem.Data.equippedSkin = skinType;
                SaveSystem.Save();
            }
            else if (!_cart.HasSkin(skinType)) // carting it already chose it to wear
            {
                if (Remaining < offer.definition.price)
                {
                    ShowBalanceError();
                    return;
                }

                _cart.AddSkin(offer.definition);
            }

            ShowBalance();
            ShowCartButtons();
            ShowSkin(offer);
        }

        private void Checkout()
        {
            var paid = _cart.Total > 0; // read first: committing empties the cart
            _cart.Commit();
            if (paid && uiSfxController) uiSfxController.PlayPurchase();
            ShowBalance();
            ShowCartButtons();
            ShowOffer(_shown);
        }

        private void ResetCart()
        {
            _cart.Clear();
            ShowBalance();
            ShowCartButtons();
            ShowOffer(_shown);
        }

        // What the wallet has left once the cart is paid for.
        private int Remaining => CurrencySettings.Balance - _cart.Total;

        private void ShowBalance()
        {
            balanceLabel.text = $"{Remaining} points";
            if (_cart.Total > 0) balanceLabel.text += $" ({_cart.Total} pending)";
        }

        // Only a cart with something in it can be paid for or dropped.
        private void ShowCartButtons()
        {
            var events = EventSystem.current;
            var focused = events ? events.currentSelectedGameObject : null;
            checkoutButton.interactable = _cart.Total > 0;
            resetCartButton.interactable = _cart.Total > 0;
            if (!focused || events.currentSelectedGameObject) return;
            events.SetSelectedGameObject(focused); // dimming the button under the focus drops it
        }

        private void ShowItem(ItemOffer itemOffer)
        {
            itemLabel.text =
                $"{itemOffer.definition.displayName}: {itemOffer.definition.price} points (Owned {OwnedCount(itemOffer.definition.type)})";
            var pending = _cart.CountOf(itemOffer.definition.type);
            if (pending > 0) itemLabel.text += $" (Pending {pending})";
            if (itemOffer.definition.price > Remaining) itemLabel.text += " Insufficient funds";
        }

        private void ShowUpgrade(ItemOffer itemOffer)
        {
            var definition = itemOffer.definition;
            var status = UpgradeSettings.IsUpgraded(definition.type) ? "Bought"
                : _cart.HasUpgrade(definition.type) ? "Pending"
                : definition.upgradePrice > Remaining ? $"{definition.upgradePrice} points Insufficient funds"
                : $"{definition.upgradePrice} points";
            itemLabel.text = $"{definition.displayName} Upgrade: {status}";
        }

        private void ShowSkin(SkinOffer offer)
        {
            var definition = offer.definition;
            var skinType = definition.type;
            var status = SaveSystem.Data.equippedSkin == skinType ? "Equipped"
                : IsBought(skinType) ? "Bought"
                : _cart.HasSkin(skinType) ? "Pending"
                : definition.price > Remaining ? $"{definition.price} points Insufficient funds"
                : $"{definition.price} points";
            itemLabel.text = $"{definition.displayName}: {status}";
        }

        private static int OwnedCount(ItemType type) => SaveSystem.Data.ownedItems.Count(k => k == type);

        // The skin the player starts in is theirs without buying.
        private static bool IsBought(SkinType type) =>
            type == SkinType.Default || SaveSystem.Data.boughtSkins.Contains(type);

        private void ShowUpgradeSprite(ItemOffer itemOffer)
        {
            if (!UpgradeSettings.IsUpgraded(itemOffer.definition.type)) return;
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