using System;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    // The use slot on the HUD: the selected item drawn over the authored square, or nothing when the slot is empty.
    public class InventoryHud : MonoBehaviour
    {
        [Serializable]
        private class Icon
        {
            public ItemDefinition definition;
            public Image image;
        }

        [SerializeField] private Icon[] icons;
        [SerializeField] private TMP_Text count;

        private void OnEnable()
        {
            PlayerInventory.OnSlotChanged += Show;
            Show(null, 0); // start empty until the inventory reports what is in the slot
        }

        private void OnDisable() => PlayerInventory.OnSlotChanged -= Show;

        // Shows the selected item type's icon and count and hides the rest; an empty slot shows none of them.
        private void Show(ItemType? selected, int held)
        {
            foreach (var icon in icons)
            {
                icon.image.gameObject.SetActive(selected.HasValue && icon.definition.type == selected.Value);
            }

            count.gameObject.SetActive(held > 0);
            count.text = held.ToString();
        }
    }
}