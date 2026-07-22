using System;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    // The use slot on the HUD: the selected item drawn over the authored square, or nothing when the slot is empty.
    // The item images are authored in the scene, one per kind; this only shows the one whose kind is selected.
    public class InventoryHud : MonoBehaviour
    {
        // One kind's icon over the use slot. The image is authored; only its active state is driven here.
        [Serializable]
        private class Icon
        {
            public ItemKind kind;
            public Image image;
        }

        [SerializeField] private Icon[] icons;

        private void OnEnable()
        {
            PlayerInventory.OnSlotChanged += Show;
            Show(null); // start empty until the inventory reports what is in the slot
        }

        private void OnDisable() => PlayerInventory.OnSlotChanged -= Show;

        // Shows the selected kind's icon and hides the rest; an empty slot shows none of them.
        private void Show(ItemKind? selected)
        {
            foreach (var icon in icons)
                icon.image.gameObject.SetActive(selected.HasValue && icon.kind == selected.Value);
        }
    }
}
