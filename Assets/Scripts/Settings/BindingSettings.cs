using UnityEngine;

namespace Settings
{
    public static class BindingSettings
    {
        private const string OverridesKey = "MoveBindingOverrides";

        public static string Overrides
        {
            get => PlayerPrefs.GetString(OverridesKey, "");
            set => PlayerPrefs.SetString(OverridesKey, value);
        }

        public static void Save() => PlayerPrefs.Save();

        public static void Clear() => PlayerPrefs.DeleteKey(OverridesKey);
    }
}