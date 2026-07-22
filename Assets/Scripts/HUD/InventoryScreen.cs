using System;
using Menu;
using Player;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HUD
{
    // The inventory screen: a panel with one slot per item kind.
    public class InventoryScreen : MonoBehaviour
    {
        // One kind's slot: the kind it stands for and the authored image tinted for it.
        [Serializable]
        private class Slot
        {
            public ItemKind kind;
            public Image icon;
        }

        [Header("UI Game Objects")] [SerializeField]
        private MenuPanel panel;

        [SerializeField] private Slot[] slots;
        [SerializeField] private TMP_Text label; // names the highlighted kind and how many are held

        [Header("Slot Colours")] [SerializeField]
        private Color slotColour = Color.white;

        [SerializeField] private Color highlightColour = Color.yellow; // the slot under the cursor
        [SerializeField, Range(0f, 1f)] private float missingAlpha = 0.25f; // faded when none of the kind is held

        [Header("UI Buttons")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button backdrop;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference openAction;
        [SerializeField] private InputActionReference leftAction;
        [SerializeField] private InputActionReference rightAction;
        [SerializeField] private InputActionReference useAction;
        [SerializeField] private InputActionReference pauseAction;

        private PlayerInventory _inventory; // bound to the spawned player each level
        private int _cursor; // index into slots of the highlighted kind
        private bool _open;

        private void OnEnable()
        {
            openAction.action.Enable();
            leftAction.action.Enable();
            rightAction.action.Enable();
            useAction.action.Enable();
            pauseAction.action.Enable();
            if (openButton) openButton.onClick.AddListener(OpenFromButton);
            if (backdrop) backdrop.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            openAction.action.Disable();
            leftAction.action.Disable();
            rightAction.action.Disable();
            useAction.action.Disable();
            pauseAction.action.Disable();
            if (openButton) openButton.onClick.RemoveListener(OpenFromButton);
            if (backdrop) backdrop.onClick.RemoveListener(Close);
        }

        // Handed the spawned player's inventory each level, the way the camera is handed its target.
        public void Bind(PlayerInventory inventory) => _inventory = inventory;

        private void Update()
        {
            if (!_open)
            {
                // Opens on its own key, but only when nothing else holds the game (a pause, a hack, etc.).
                if (openAction.action.WasPressedThisFrame() && _inventory && !GameLock.Locked) Open();
                return;
            }

            if (leftAction.action.WasPressedThisFrame()) MoveCursor(-1);
            if (rightAction.action.WasPressedThisFrame()) MoveCursor(1);
            if (useAction.action.WasPressedThisFrame()) Choose();
            if (pauseAction.action.WasPressedThisFrame()) Close();
            if (openAction.action.WasPressedThisFrame()) Close();
        }

        // The on-screen button opens the screen the same way its key does.
        private void OpenFromButton()
        {
            if (!_open && _inventory && !GameLock.Locked) Open();
        }

        // Presents the slots and parks the cursor on the kind already in the use slot.
        private void Open()
        {
            _open = true;
            GameLock.Acquire();
            panel.SetActive(true);
            if (backdrop) backdrop.gameObject.SetActive(true);
            if (openButton) openButton.gameObject.SetActive(false);
            _cursor = CursorFor(_inventory.Selected);
            Redraw();
        }

        // Steps the cursor along the row, stopping at the ends.
        private void MoveCursor(int step)
        {
            _cursor = Mathf.Clamp(_cursor + step, 0, slots.Length - 1);
            Redraw();
        }

        // Puts the highlighted kind into the use slot and closes.
        private void Choose()
        {
            var kind = slots[_cursor].kind;
            if (_inventory.CountOf(kind) == 0) return;
            _inventory.Select(kind);
            Close();
        }

        private void Close()
        {
            if (!_open) return;
            _open = false;
            panel.SetActive(false);
            if (backdrop) backdrop.gameObject.SetActive(false);
            if (openButton) openButton.gameObject.SetActive(true);
            GameLock.Release();
        }

        // Tints each slot for the cursor, fades the kinds held none of, and names the highlighted kind and its count.
        private void Redraw()
        {
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var held = _inventory.CountOf(slot.kind);
                slot.icon.color = Fade(i == _cursor ? highlightColour : slotColour, held > 0 ? 1f : missingAlpha);
                if (i == _cursor) label.text = $"{NameOf(slot.kind)}: {held}";
            }
        }

        // The slot index for a kind, defaulting to the first slot when the use slot is empty.
        private int CursorFor(ItemKind? selected)
        {
            if (!selected.HasValue) return 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].kind == selected.Value) return i;
            }
            return 0;
        }

        // The spaced name for a kind, since the enum runs the words together.
        private static string NameOf(ItemKind kind) => kind switch
        {
            ItemKind.LockPick => "Lock Pick",
            ItemKind.HealthPack => "Health Pack",
            _ => kind.ToString(),
        };

        private static Color Fade(Color colour, float alpha) => new(colour.r, colour.g, colour.b, alpha);
    }
}
