using System;
using System.Linq;
using Player;
using Run;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // The shop panel: spends banked currency on the items the next run starts with.
    public class ShopMenu : MonoBehaviour
    {
        // One item kind's offer: its authored price and the button that buys it.
        [Serializable]
        private class Offer
        {
            public ItemKind kind;
            public int price;
            public Button button;
        }

        [SerializeField] private TMP_Text balanceLabel;
        [SerializeField] private TMP_Text itemLabel; // the highlighted item's price and how many are owned
        [SerializeField] private Offer[] offers;

        private GameObject _highlighted;

        private void Start()
        {
            foreach (var offer in offers) offer.button.onClick.AddListener(() => Buy(offer));
            ShowBalance();
        }

        // Refreshes the shared label when the highlight moves to another item.
        private void Update()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _highlighted) return;
            _highlighted = selected;
            var offer = offers.FirstOrDefault(o => o.button.gameObject == selected);
            if (offer != null) ShowItem(offer);
        }

        // Buys one of the kind, spending its price, unless the wallet can't afford it.
        private void Buy(Offer offer)
        {
            if (CurrencySettings.Balance < offer.price) return;

            CurrencySettings.Balance -= offer.price;
            CurrencySettings.Save();
            (RunLoadout.Pending ??= new RunLoadout()).Add(offer.kind);
            ShowBalance();
            ShowItem(offer);
        }

        private void ShowBalance() => balanceLabel.text = $"{CurrencySettings.Balance} points";

        private void ShowItem(Offer offer) =>
            itemLabel.text = $"{NameOf(offer.kind)}: {offer.price} points (Owned {OwnedCount(offer.kind)})";

        private static int OwnedCount(ItemKind kind) => RunLoadout.Pending?.Items.Count(k => k == kind) ?? 0;

        // The spaced name for a kind, since the enum runs the words together.
        private static string NameOf(ItemKind kind) => kind switch
        {
            ItemKind.LockPick => "Lock Pick",
            ItemKind.HealthPack => "Health Pack",
            _ => kind.ToString(),
        };
    }
}
