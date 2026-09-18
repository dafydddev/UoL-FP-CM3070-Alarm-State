using UnityEngine.InputSystem;

namespace Settings
{
    public static class BindingDisplay
    {
        // The label an action carries on the device in use, empty where it has none there.
        public static string For(InputAction action, InputDeviceState state)
        {
            var gamepad = BindingSettings.AutoDetectDevice
                ? state && state.CurrentDevice is Gamepad
                : BindingSettings.DeviceIndex == BindingSettings.GamepadOption;

            var path = gamepad ? "<Gamepad>" : "<Keyboard>";
            var index = action.bindings.IndexOf(b => b.path.StartsWith(path));

            return index < 0
                ? ""
                : action
                    .GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions)
                    .ToUpper();
        }
    }
}
