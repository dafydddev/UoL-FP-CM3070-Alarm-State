using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Settings
{
    public class InputDeviceState : MonoBehaviour
    {
        public InputDevice CurrentDevice { get; private set; } = Keyboard.current;
        
        public event Action<InputDevice> InputTypeChanged;
        
        private void OnEnable() => InputSystem.onEvent += OnInputEvent;
        private void OnDisable() => InputSystem.onEvent -= OnInputEvent;

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            var inputDevice = device switch
            {
                Gamepad => Gamepad.current,
                Keyboard => Keyboard.current,
                _ => CurrentDevice
            };

            if (inputDevice == CurrentDevice) return;

            CurrentDevice = inputDevice;
            InputTypeChanged?.Invoke(inputDevice);
        }
    }
}