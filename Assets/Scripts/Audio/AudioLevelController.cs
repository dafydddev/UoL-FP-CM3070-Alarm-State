using Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    // Puts the saved levels on the mixer. Runtime values do not stick to the asset, so each scene applies them.
    public class AudioLevelController : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;

        // The names exposed on the mixer.
        private const string MasterParameter = "MasterVolume";
        private const string MusicParameter = "MusicVolume";
        private const string SfxParameter = "SfxVolume";

        private void Start()
        {
            SetLevel(MasterParameter, SoundSettings.Master);
            SetLevel(MusicParameter, SoundSettings.Music);
            SetLevel(SfxParameter, SoundSettings.Sfx);
        }

        private void OnEnable()
        {
            SoundSettings.MasterChanged += OnMasterChanged;
            SoundSettings.MusicChanged += OnMusicChanged;
            SoundSettings.SfxChanged += OnSfxChanged;
        }

        private void OnDisable()
        {
            SoundSettings.MasterChanged -= OnMasterChanged;
            SoundSettings.MusicChanged -= OnMusicChanged;
            SoundSettings.SfxChanged -= OnSfxChanged;
        }

        private void OnMasterChanged(float volume) => SetLevel(MasterParameter, volume);

        private void OnMusicChanged(float volume) => SetLevel(MusicParameter, volume);

        private void OnSfxChanged(float volume) => SetLevel(SfxParameter, volume);

        // Sliders read 0 to 1. The mixer wants decibels: silent at 0, untouched at 1.
        private void SetLevel(string parameter, float volume) =>
            mixer.SetFloat(parameter, Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
    }
}