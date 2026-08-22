using System;
using UnityEngine;

namespace Settings
{
    // SoundSettings to avoid clashing with UnityEngine.AudioSettings.
    public static class SoundSettings
    {
        private const string MasterKey = "MasterVolume";
        private const string MusicKey = "MusicVolume";
        private const string SfxKey = "SfxVolume";
        private const string UiKey = "UiVolume";
        private const float DefaultMaster = 0.8f;
        private const float DefaultMusic = 0.2f;
        private const float DefaultUi = 0.5f;
        private const float DefaultSfx = 0.5f;
        
        // Raised on change, so the mixer follows without a scene reload.
        public static event Action<float> MasterChanged;
        public static event Action<float> MusicChanged;
        public static event Action<float> SfxChanged;
        public static event Action<float> UiChanged;

        public static float Master
        {
            get => PlayerPrefs.GetFloat(MasterKey, DefaultMaster);
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(Master, value)) return;
                PlayerPrefs.SetFloat(MasterKey, value);
                MasterChanged?.Invoke(value);
            }
        }

        public static float Music
        {
            get => PlayerPrefs.GetFloat(MusicKey, DefaultMusic);
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(Music, value)) return;
                PlayerPrefs.SetFloat(MusicKey, value);
                MusicChanged?.Invoke(value);
            }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(Sfx, value)) return;
                PlayerPrefs.SetFloat(SfxKey, value);
                SfxChanged?.Invoke(value);
            }
        }
        
        public static float Ui
        {
            get => PlayerPrefs.GetFloat(UiKey, DefaultUi);
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(Ui, value)) return;
                PlayerPrefs.SetFloat(UiKey, value);
                UiChanged?.Invoke(value);
            }
        }

        public static void Save() => PlayerPrefs.Save();
    }
}