using UnityEngine;

namespace Settings
{
    public static class RumbleSettings
    {
        private const string EnabledKey = "RumbleEnabled";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
