using System;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    // Lets the player rebind keys.
    // Each entry is one action with a button that shows its current key and, when clicked, waits for a replacement.
    // Keys already used by another entry are refused.
    // Only keyboard bindings are rebound — gamepad bindings are fixed.
    public class RebindMenu : MonoBehaviour
    {
        // One row's UI: the action it rebinds, its button and its key label.
        [Serializable]
        private class Entry
        {
            public InputActionReference action;
            public Button button;
            public TMP_Text label;
        }

        [SerializeField] private Entry[] entries;

        // Restores every entry to the asset's default key.
        public Button resetButton;

        // The rebind currently listening for input, or null when idle.
        private InputActionRebindingExtensions.RebindingOperation _rebind;

        // The entry being changed, its binding index and its key beforehand,
        // so a clashing rebind can be undone.
        private Entry _activeEntry;
        private int _activeIndex;
        private string _oldPath;

        // Every entry's action lives in the same asset, so any entry can provide it.
        private InputActionAsset Asset => entries[0].action.action.actionMap.asset;

        private void Start()
        {
            // Restore any previously saved keys before showing them.
            // Loading on the asset covers every entry's action at once.
            var json = BindingSettings.Overrides;
            if (!string.IsNullOrEmpty(json)) Asset.LoadBindingOverridesFromJson(json);

            // Wire each button to rebind its own entry, then show the current keys.
            foreach (var entry in entries)
            {
                var captured = entry;
                entry.button.onClick.AddListener(() => StartRebind(captured));
            }

            if (resetButton) resetButton.onClick.AddListener(ResetBindings);

            RefreshLabels();
        }

        // Begin listening for a new key for one entry.
        private void StartRebind(Entry entry)
        {
            // Ignore the click if another rebind is already in progress.
            if (_rebind != null) return;

            _activeIndex = KeyboardIndex(entry.action.action);
            if (_activeIndex < 0) return;
            _activeEntry = entry;

            // Remember the current key so it can be restored if the new one clashes.
            _oldPath = entry.action.action.bindings[_activeIndex].effectivePath;

            entry.label.text = "...";

            // Disable every entry's action while listening: the action being rebound must be
            // disabled, and the others would otherwise fire off the key being pressed.
            foreach (var r in entries) r.action.action.Disable();

            _rebind = entry.action.action.PerformInteractiveRebinding(_activeIndex)
                .WithControlsExcluding("<Mouse>") // never capture the mouse as a binding
                .WithControlsExcluding("<Gamepad>") // this slot is keyboard-only
                .OnComplete(_ => Complete())
                .OnCancel(_ => Finish())
                .Start();
        }

        // Called once a key has been chosen.
        private void Complete()
        {
            // Refuse the new key if another entry already uses it, undoing this binding only.
            var action = _activeEntry.action.action;
            var newPath = action.bindings[_activeIndex].effectivePath;
            if (IsUsedByAnotherEntry(newPath)) action.ApplyBindingOverride(_activeIndex, _oldPath);

            // Persist the new keys so they survive the next scene load.
            BindingSettings.Overrides = Asset.SaveBindingOverridesAsJson();
            BindingSettings.Save();
            Finish();
        }

        // Clean up the operation, re-enable the actions and refresh the labels.
        private void Finish()
        {
            _rebind?.Dispose();
            _rebind = null;
            _activeEntry = null;

            foreach (var entry in entries) entry.action.action.Enable();

            RefreshLabels();
        }

        // Drop every custom key and fall back to the asset's defaults.
        private void ResetBindings()
        {
            // Ignore the click while a rebind is listening for input.
            if (_rebind != null) return;

            foreach (var entry in entries) entry.action.action.RemoveAllBindingOverrides();
            BindingSettings.Clear();
            BindingSettings.Save();
            RefreshLabels();
        }

        // True if any other entry's keyboard binding uses the same key.
        private bool IsUsedByAnotherEntry(string path)
        {
            foreach (var entry in entries)
            {
                if (entry == _activeEntry) continue;
                var index = KeyboardIndex(entry.action.action);
                if (index >= 0 && entry.action.action.bindings[index].effectivePath == path) return true;
            }

            return false;
        }

        // Update every button's label to show the key currently bound to its entry.
        private void RefreshLabels()
        {
            foreach (var entry in entries)
            {
                var index = KeyboardIndex(entry.action.action);
                entry.label.text = index < 0
                    ? ""
                    : entry.action.action
                        .GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                        .ToUpper();
            }
        }

        // The keyboard binding for an action. Found by its default path, so it stays the same
        // binding after an override is applied and other device bindings are never touched.
        private static int KeyboardIndex(InputAction action) =>
            action.bindings.IndexOf(b => b.path.StartsWith("<Keyboard>"));
    }
}