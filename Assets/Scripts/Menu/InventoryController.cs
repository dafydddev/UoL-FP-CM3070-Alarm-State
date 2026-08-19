using System;
using System.Linq;
using Player;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    // The inventory screen: a panel with one slot button per item.
    public class InventoryController : MonoBehaviour
    {
        // An inventory slot: the item definition it uses and the button that picks it.
        [Serializable]
        private class Slot
        {
            public ItemDefinition definition;
            public Button button;
        }

        [Header("UI Game Objects")] [SerializeField]
        private MenuPanel panel;

        [SerializeField] private Slot[] slots;
        [SerializeField] private TMP_Text sharedLabel; // UI label for the highlighted item

        [Header("UI Buttons")] [SerializeField]
        private Button openButton;

        [SerializeField] private Button backdrop;

        [Header("Input Actions")] [SerializeField]
        private InputActionReference openAction;

        [SerializeField] private InputActionReference pauseAction;

        private PlayerInventory _inventory; // bound to the spawned player each level
        private GameObject _highlighted;
        private bool _open;

        private void OnEnable()
        {
            openAction.action.Enable();
            pauseAction.action.Enable();
            if (openButton) openButton.onClick.AddListener(OpenFromButton);
            if (backdrop) backdrop.onClick.AddListener(Close);
            foreach (var slot in slots) slot.button.onClick.AddListener(() => Choose(slot));
        }

        private void OnDisable()
        {
            openAction.action.Disable();
            pauseAction.action.Disable();
            if (openButton) openButton.onClick.RemoveListener(OpenFromButton);
            if (backdrop) backdrop.onClick.RemoveListener(Close);
            foreach (var slot in slots) slot.button.onClick.RemoveAllListeners();
        }

        // Handed the spawned player's inventory each level, the way the camera is handed its target.
        public void Bind(PlayerInventory inventory) => _inventory = inventory;

        private void Update()
        {
            if (!_open)
            {
                // Opens on its own key, but only when nothing else holds the game (a pause, a minigame, etc.).
                if (openAction.action.WasPressedThisFrame() && CanOpen) Open();
                return;
            }

            ShowHighlighted();
            if (pauseAction.action.WasPressedThisFrame()) Close();
            if (openAction.action.WasPressedThisFrame()) Close();
        }

        private bool CanOpen => !_open && _inventory && !GameLock.Locked && !InputCapture.Captured;

        // The on-screen button opens the screen the same way its key does.
        private void OpenFromButton()
        {
            if (CanOpen) Open();
        }

        // Presents the slots and focuses the kind already in the use slot.
        private void Open()
        {
            _open = true;
            GameLock.Acquire();
            panel.SetActive(true);
            if (backdrop) backdrop.gameObject.SetActive(true);
            if (openButton) openButton.gameObject.SetActive(false);

            // Kinds held none of can't be picked; the disabled tint fades them.
            foreach (var slot in slots) slot.button.interactable = _inventory.CountOf(slot.definition.kind) > 0;

            // Draw the label before the panel's first frame, not on the next poll.
            var selected = SlotFor(_inventory.Selected);
            selected.button.Select();
            _highlighted = selected.button.gameObject;
            ShowSlot(selected);
        }

        // Puts the picked kind into the use slot and closes.
        private void Choose(Slot slot)
        {
            _inventory.Select(slot.definition.kind);
            Close();
        }

        private void Close()
        {
            if (!_open) return;
            _open = false;
            panel.SetActive(false);
            if (backdrop) backdrop.gameObject.SetActive(false);
            if (openButton) openButton.gameObject.SetActive(true);
            // Drop focus so a stray navigation input mid-run can't re-fire a slot.
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            _highlighted = null;
            GameLock.Release();
        }

        // Refreshes the shared label when the highlight moves to another slot.
        private void ShowHighlighted()
        {
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == _highlighted) return;
            _highlighted = selected;
            var slot = slots.FirstOrDefault(s => s.button.gameObject == selected);
            if (slot != null) ShowSlot(slot);
        }

        private void ShowSlot(Slot slot) => sharedLabel.text =
            $"{slot.definition.displayName}: {_inventory.CountOf(slot.definition.kind)}";

        // The slot for a kind, defaulting to the first when the use slot is empty.
        private Slot SlotFor(ItemKind? selected)
        {
            if (!selected.HasValue) return slots[0];
            return slots.FirstOrDefault(s => s.definition.kind == selected.Value) ?? slots[0];
        }
    }
}