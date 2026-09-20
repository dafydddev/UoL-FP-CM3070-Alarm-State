using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    // The prompt above the player, shown while they stand on something they can use.
    // The sprite is the bubble behind. The label on it shows the current binding of the useAction.
    public class UsePrompt : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer prompt;
        [SerializeField] private TMP_Text label;
        [SerializeField] private InputActionReference useAction;

        private bool _shown;
        private InputDeviceState _deviceState;

        private void Awake()
        {
            _shown = prompt.enabled;
            if (label) label.enabled = _shown;
        }

        private void OnEnable()
        {
            if (_deviceState) _deviceState.InputTypeChanged += OnDeviceChanged;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_deviceState) _deviceState.InputTypeChanged -= OnDeviceChanged;
        }

        // Handed over by the spawner. The player is a prefab and cannot hold a scene reference.
        public void Bind(InputDeviceState deviceState)
        {
            if (_deviceState) _deviceState.InputTypeChanged -= OnDeviceChanged;
            _deviceState = deviceState;
            if (_deviceState && isActiveAndEnabled) _deviceState.InputTypeChanged += OnDeviceChanged;
            RefreshLabel();
        }

        public void Show(bool shown)
        {
            if (shown == _shown) return;
            _shown = shown;
            prompt.enabled = shown;
            if (label) label.enabled = shown;
        }

        private void OnDeviceChanged(InputDevice device) => RefreshLabel();

        // Read at the point of binding and on a device swap, so the glyph matches the keys in hand.
        private void RefreshLabel()
        {
            if (!label || !useAction) return;
            label.text = BindingDisplay.For(useAction.action, _deviceState);
        }
    }
}