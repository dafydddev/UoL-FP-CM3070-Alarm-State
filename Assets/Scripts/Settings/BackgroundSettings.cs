using System;
using UnityEngine;

namespace Settings
{
    // Whether the menu backdrop drifts. Shared across scenes, on by default.
    public static class BackgroundSettings
    {
        private const string ScrollingKey = "ScrollingBackground";

        // Raised on change, so backdrops already loaded stop or resume without a scene reload.
        public static event Action<bool> ScrollingChanged;

        public static bool Scrolling
        {
            get => PlayerPrefs.GetInt(ScrollingKey, 1) == 1;
            set
            {
                if (Scrolling == value) return;
                PlayerPrefs.SetInt(ScrollingKey, value ? 1 : 0);
                ScrollingChanged?.Invoke(value);
            }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
