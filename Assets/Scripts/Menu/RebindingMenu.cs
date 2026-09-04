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

        [SerializeField] private Toggle autoDetectToggle;
        [SerializeField] private TMP_Dropdown deviceDropdown;

        [SerializeField] private Entry[] entries;

        [SerializeField] private Button resetButton;

        private InputActionRebindingExtensions.RebindingOperation _rebind;

        private Entry _activeEntry;
        private int _activeIndex;
        private string _oldPath;

        private InputActionAsset Asset => entries[0].action.action.actionMap.asset;

        private bool _autoDetect;
        private bool _useGamepad;

        private void OnEnable()
        {
            inputDeviceState.InputTypeChanged += OnInputEvent;
            foreach (var entry in entries)
            {
                var captured = entry;
                entry.button.onClick.AddListener(() => StartRebind(captured));
            }

            resetButton?.onClick.AddListener(ResetBindings);
            if (autoDetectToggle) autoDetectToggle.onValueChanged.AddListener(OnAutoDetectChanged);
            if (deviceDropdown) deviceDropdown.onValueChanged.AddListener(OnDeviceDropdownChanged);
        }

        private void OnDisable()
        {
            inputDeviceState.InputTypeChanged -= OnInputEvent;
            foreach (var entry in entries)
            {
                entry.button.onClick.RemoveListener(() => StartRebind(entry));
            }

            resetButton?.onClick.RemoveListener(ResetBindings);
            if (autoDetectToggle) autoDetectToggle.onValueChanged.RemoveListener(OnAutoDetectChanged);
            if (deviceDropdown) deviceDropdown.onValueChanged.RemoveListener(OnDeviceDropdownChanged);
        }

        private void Start()
        {
            LoadDeviceSettings();
            RefreshLabels();
            RefreshImages();
        }

        private void LoadDeviceSettings()
        {
            _autoDetect = BindingSettings.AutoDetectDevice;
            var index = Mathf.Clamp(BindingSettings.DeviceIndex, BindingSettings.KeyboardOption,
                BindingSettings.GamepadOption);

            if (deviceDropdown)
            {
                deviceDropdown.SetValueWithoutNotify(index);
                deviceDropdown.RefreshShownValue();
            }

            autoDetectToggle?.SetIsOnWithoutNotify(_autoDetect);

            _useGamepad = _autoDetect
                ? inputDeviceState.CurrentDevice is Gamepad
                : index == BindingSettings.GamepadOption;

            // Repair the saved value if it was invalid.
            BindingSettings.DeviceIndex = index;
            SyncDropdownInteractable();
        }

        private void OnAutoDetectChanged(bool value)
        {
            _autoDetect = value;
            BindingSettings.AutoDetectDevice = value;
            BindingSettings.Save();

            // Fall back to whichever source now owns the selection.
            SetUseGamepad(value
                ? inputDeviceState.CurrentDevice is Gamepad
                : deviceDropdown && deviceDropdown.value == BindingSettings.GamepadOption);

            SyncDropdownInteractable();
        }

        private void OnDeviceDropdownChanged(int index)
        {
            BindingSettings.DeviceIndex = index;
            BindingSettings.Save();
            if (_autoDetect) return;
            SetUseGamepad(index == BindingSettings.GamepadOption);
        }

        private void OnInputEvent(InputDevice deviceType)
        {
            if (!_autoDetect) return;
            SetUseGamepad(deviceType is Gamepad);
        }

        private void SetUseGamepad(bool useGamepad)
        {
            _useGamepad = useGamepad;
            SyncDropdownValue();
            RefreshLabels();
            RefreshImages();
        }

        // Keep the dropdown showing the device in use while auto-detecting.
        private void SyncDropdownValue()
        {
            if (!deviceDropdown || !_autoDetect) return;
            deviceDropdown.SetValueWithoutNotify(_useGamepad
                ? BindingSettings.GamepadOption
                : BindingSettings.KeyboardOption);
            deviceDropdown.RefreshShownValue();
        }

        private void SyncDropdownInteractable()
        {
            if (deviceDropdown) deviceDropdown.interactable = !_autoDetect;
        }

        private void StartRebind(Entry entry)
        {
            if (_rebind != null) return;
            _activeIndex = BindingIndex(entry.action.action);
            if (_activeIndex < 0) return;
            // Disable navigation while rebinding.
            uiNavigateReference?.action.Disable();
            _activeEntry = entry;
            _oldPath = entry.action.action.bindings[_activeIndex].effectivePath;
            entry.label.text = "...";
            // Disable every bindable action, the one being rebound included, so the press only feeds the rebind.
            foreach (var r in entries)
            {
                r.action.action.Disable();
            }

            _rebind = entry.action.action.PerformInteractiveRebinding(_activeIndex)
                .WithControlsHavingToMatchPath(DevicePath)
                .WithControlsExcluding("<Mouse>/*")
                .OnComplete(_ => Complete())
                .OnCancel(_ => Finish())
                .Start();
        }

        private void Complete()
        {
            var action = _activeEntry.action.action;
            var newPath = action.bindings[_activeIndex].effectivePath;

            // A control another entry already holds is refused outright rather than swapped with it.
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
            activeImage.sprite = _useGamepad ? gamepadSprite : keyboardSprite;
        }

        private string DevicePath => _useGamepad ? "<Gamepad>" : "<Keyboard>";

        // The action's first binding for the device in use, or -1 where it has none.
        private int BindingIndex(InputAction action)
        {
            var path = DevicePath;

            return action.bindings.IndexOf(b => b.path.StartsWith(path));
        }
    }
}