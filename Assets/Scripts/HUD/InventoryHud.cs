using System;
using Player;
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

        private void OnEnable()
        {
            PlayerInventory.OnSlotChanged += Show;
            Show(null); // start empty until the inventory reports what is in the slot
        }

        private void OnDisable() => PlayerInventory.OnSlotChanged -= Show;

        // Shows the selected item type's icon and hides the rest; an empty slot shows none of them.
        private void Show(ItemType? selected)
        {
            foreach (var icon in icons)
            {
                icon.image.gameObject.SetActive(selected.HasValue && icon.definition.type == selected.Value);
            }
        }
    }
}