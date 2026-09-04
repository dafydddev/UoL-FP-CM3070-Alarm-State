using UnityEngine;

namespace Settings
{
    public static class BindingSettings
    {
        private const string OverridesKey = "MoveBindingOverrides";
        private const string AutoDetectKey = "BindingAutoDetectDevice";
        private const string DeviceIndexKey = "BindingDeviceIndex";

        public static string Overrides
        {
            get => PlayerPrefs.GetString(OverridesKey, "");
            set => PlayerPrefs.SetString(OverridesKey, value);
        }

        // Detect the active device from input events instead of the dropdown.
        public static bool AutoDetectDevice
        {
            get => PlayerPrefs.GetInt(AutoDetectKey, 0) == 1;
            set => PlayerPrefs.SetInt(AutoDetectKey, value ? 1 : 0);
        }

        public const int KeyboardOption = 0;
        public const int GamepadOption = 1;

        public static int DeviceIndex
        {
            get => PlayerPrefs.GetInt(DeviceIndexKey, 0);
            set => PlayerPrefs.SetInt(DeviceIndexKey, value);
        }

        // The setters only write to the in-memory prefs, so a change is lost unless this is called.
        public static void Save() => PlayerPrefs.Save();

        // Drops the overrides alone; the device settings stand.
        public static void Clear() => PlayerPrefs.DeleteKey(OverridesKey);
    }
}