using System;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    // Lets the player rebind the four movement keys. Each row is one direction with a button
    // that shows its current key and, when clicked, waits for a replacement.
    // Keys already used by another direction are refused; arrow keys are valid replacements.
    public class RebindMenu : MonoBehaviour
    {
        // One direction's UI: the composite part it controls, its button and its key label.
        [Serializable]
        private class Row
        {
            public string direction; // "up", "down", "left", "right"
            public Button button;
            public TMP_Text label;
        }

        // The Move action in the InputSystem_Actions asset (the same one the player reads).
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private Row[] rows;

        // Restores every direction to the asset's default key.
        public Button resetButton;

        // The rebind currently listening for input, or null when idle.
        private InputActionRebindingExtensions.RebindingOperation _rebind;

        // The binding being changed and its key beforehand, so a clashing rebind can be undone.
        private int _activeIndex;
        private string _oldPath;
        
        private void Start()
        {
            // Restore any previously saved keys before showing them.
            var json = BindingSettings.Overrides;
            if (!string.IsNullOrEmpty(json)) moveAction.action.LoadBindingOverridesFromJson(json);

            // Wire each button to rebind its own direction, then show the current keys.
            foreach (var row in rows)
            {
                var captured = row;
                row.button.onClick.AddListener(() => StartRebind(captured));
            }

            if (resetButton) resetButton.onClick.AddListener(ResetBindings);

            RefreshLabels();
        }
        
        // Begin listening for a new key for one direction.
        private void StartRebind(Row row)
        {
            // Ignore the click if another rebind is already in progress.
            if (_rebind != null) return;

            _activeIndex = IndexOf(row.direction);
            if (_activeIndex < 0) return;

            // Remember the current key so it can be restored if the new one clashes.
            _oldPath = moveAction.action.bindings[_activeIndex].effectivePath;

            row.label.text = "...";
            // The action must be disabled while interactive rebinding runs.
            moveAction.action.Disable();
            _rebind = moveAction.action.PerformInteractiveRebinding(_activeIndex)
                .WithControlsExcluding("<Mouse>") // never capture the mouse as a binding
                .OnComplete(_ => Complete())
                .OnCancel(_ => Finish())
                .Start();
        }

        // Called once a key has been chosen.
        private void Complete()
        {
            // Refuse the new key if another direction already uses it, undoing this binding only.
            var newPath = moveAction.action.bindings[_activeIndex].effectivePath;
            if (IsUsedByAnotherDirection(newPath)) moveAction.action.ApplyBindingOverride(_activeIndex, _oldPath);

            // Persist the new keys so they survive the next scene load.
            BindingSettings.Overrides = moveAction.action.SaveBindingOverridesAsJson();
            BindingSettings.Save();
            Finish();
        }

        // Clean up the operation, re-enable the action and refresh the labels.
        private void Finish()
        {
            _rebind?.Dispose();
            _rebind = null;
            moveAction.action.Enable();
            RefreshLabels();
        }

        // Drop every custom key and fall back to the asset's defaults.
        private void ResetBindings()
        {
            // Ignore the click while a rebind is listening for input.
            if (_rebind != null) return;

            moveAction.action.RemoveAllBindingOverrides();
            BindingSettings.Clear();
            BindingSettings.Save();
            RefreshLabels();
        }

        // True if any other direction is bound to the same key. Only the four directions are
        // compared, so the asset's arrow-key parts don't block binding a direction to an arrow.
        private bool IsUsedByAnotherDirection(string path)
        {
            foreach (var row in rows)
            {
                var index = IndexOf(row.direction);
                if (index == _activeIndex) continue;
                if (moveAction.action.bindings[index].effectivePath == path) return true;
            }

            return false;
        }

        // Update every button's label to show the key currently bound to its direction.
        private void RefreshLabels()
        {
            foreach (var row in rows)
            {
                var index = IndexOf(row.direction);
                row.label.text = index < 0 ? "" : moveAction.action.GetBindingDisplayString(index).ToUpper();
            }
        }

        // The composite part binding for a direction, found by its part name.
        private int IndexOf(string direction) =>
            moveAction.action.bindings.IndexOf(b =>
                b.isPartOfComposite && string.Equals(b.name, direction, StringComparison.OrdinalIgnoreCase));
    }
}