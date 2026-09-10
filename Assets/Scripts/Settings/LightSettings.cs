using System;
using UnityEngine;

namespace Settings
{
    public static class LightSettings
    {
        private const string LightingKey = "Lighting";

        // Raised on change, so a level already built relights without a scene reload.
        public static event Action<bool> LightingChanged;

        public static bool Lighting
        {
            get => PlayerPrefs.GetInt(LightingKey, 0) == 1;
            set
            {
                if (Lighting == value) return;
                PlayerPrefs.SetInt(LightingKey, value ? 1 : 0);
                LightingChanged?.Invoke(value);
            }
        }

        // The setter only writes to the in-memory prefs, so a change is lost unless this is called.
        public static void Save() => PlayerPrefs.Save();
    }
}
