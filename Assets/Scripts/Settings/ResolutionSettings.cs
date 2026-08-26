using UnityEngine;

namespace Settings
{
    public static class ResolutionSettings
    {
        private const string ResolutionKey = "ResolutionIndex";
        private const string FullscreenKey = "Fullscreen";

        // An index into the resolution list the graphics options screen offers.
        public static int ResolutionIndex
        {
            get => PlayerPrefs.GetInt(ResolutionKey, 2);
            set => PlayerPrefs.SetInt(ResolutionKey, value);
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
            set => PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        }

        // The setters only write to the in-memory prefs, so a change is lost unless this is called.
        public static void Save() => PlayerPrefs.Save();
    }
}