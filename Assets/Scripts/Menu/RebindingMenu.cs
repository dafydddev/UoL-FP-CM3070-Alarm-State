using System;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Menu
{
    public class RebindMenu : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            public InputActionReference action;
            public Button button;
            public TMP_Text label;
        }

        [SerializeField] private InputDeviceState inputDeviceState;
        [SerializeField] private InputActionReference uiNavigateReference;

        [SerializeField] private Sprite keyboardSprite;
        [SerializeField] private Sprite gamepadSprite;
        [SerializeField] private Image activeImage;

        [SerializeField] private Entry[] entries;

        [SerializeField] private Button resetButton;

        private InputActionRebindingExtensions.RebindingOperation _rebind;

        private Entry _activeEntry;
        private int _activeIndex;
        private string _oldPath;

        private InputActionAsset Asset => entries[0].action.action.actionMap.asset;

        private InputDevice _activeDevice;

        private void OnEnable()
        {
            inputDeviceState.InputTypeChanged += OnInputEvent;
            foreach (var entry in entries)
            {
                var captured = entry;
                entry.button.onClick.AddListener(() => StartRebind(captured));
            }
            resetButton?.onClick.AddListener(ResetBindings);
        }

        private void OnDisable()
        {
            inputDeviceState.InputTypeChanged -= OnInputEvent;
            foreach (var entry in entries)
            {
                entry.button.onClick.RemoveListener(() => StartRebind(entry));
            }
            resetButton?.onClick.RemoveListener(ResetBindings);
        }

        private void Start()
        {
            _activeDevice = inputDeviceState.CurrentDevice;
            var json = BindingSettings.Overrides;
            if (!string.IsNullOrEmpty(json)) Asset.LoadBindingOverridesFromJson(json);
            RefreshLabels();
            RefreshImages();
        }

        private void OnInputEvent(InputDevice deviceType)
        {
            _activeDevice = deviceType;
            RefreshLabels();
            RefreshImages();
        }

        private void StartRebind(Entry entry)
        {
            if (_rebind != null) return;
            _activeIndex = BindingIndex(entry.action.action);
            if (_activeIndex < 0) return;
            // Disable navigation while rebinding.
            uiNavigateReference?.action.Disable();
            // Disable all other bindings.
            _activeEntry = entry;
            _oldPath = entry.action.action.bindings[_activeIndex].effectivePath;
            entry.label.text = "...";
            foreach (var r in entries)
            {
                r.action.action.Disable();
            }
            var path = _activeDevice is Gamepad ? "<Gamepad>" : "<Keyboard>";
            _rebind = entry.action.action.PerformInteractiveRebinding(_activeIndex)
                .WithControlsHavingToMatchPath(path)
                .WithControlsExcluding("<Mouse>/*")
                .OnComplete(_ => Complete())
                .OnCancel(_ => Finish())
                .Start();
        }

        private void Complete()
        {
            var action = _activeEntry.action.action;
            var newPath = action.bindings[_activeIndex].effectivePath;

            if (IsUsedByAnotherEntry(newPath)) action.ApplyBindingOverride(_activeIndex, _oldPath);

            BindingSettings.Overrides = Asset.SaveBindingOverridesAsJson();
            BindingSettings.Save();

            Finish();
        }

        private void Finish()
        {
            _rebind?.Dispose();
            _rebind = null;
            _activeEntry = null;

            foreach (var entry in entries)
            {
                entry.action.action.Enable();
            }
            RefreshLabels();
            // Re-enable navigation.
            uiNavigateReference?.action.Enable();
            // Swallow any remaining events, such as the joystick moving.
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (!selected) return;
            if (!selected.TryGetComponent<MoveGuard>(out var guard)) guard = selected.AddComponent<MoveGuard>();
            guard.Arm();
        }

        private void ResetBindings()
        {
            if (_rebind != null) return;

            foreach (var entry in entries)
            {
                entry.action.action.RemoveAllBindingOverrides();
            }

            BindingSettings.Clear();
            BindingSettings.Save();
            RefreshLabels();
        }

        private bool IsUsedByAnotherEntry(string path)
        {
            foreach (var entry in entries)
            {
                if (entry == _activeEntry) continue;
                var index = BindingIndex(entry.action.action);
                if (index >= 0 && entry.action.action.bindings[index].effectivePath == path) return true;
            }
            return false;
        }

        private void RefreshLabels()
        {
            foreach (var entry in entries)
            {
                var index = BindingIndex(entry.action.action);
                entry.label.text = index < 0
                    ? ""
                    : entry.action.action
                        .GetBindingDisplayString(
                            index,
                            InputBinding.DisplayStringOptions.DontIncludeInteractions)
                        .ToUpper();
            }
        }

        private void RefreshImages()
        {
            activeImage.sprite = _activeDevice is Gamepad ? gamepadSprite : keyboardSprite;
        }

        private int BindingIndex(InputAction action)
        {
            var path = _activeDevice is Gamepad
                ? "<Gamepad>"
                : "<Keyboard>";

            return action.bindings.IndexOf(b => b.path.StartsWith(path));
        }
    }
}
