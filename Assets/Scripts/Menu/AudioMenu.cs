using Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class AudioMenu : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Button applyButton;

        private float _master;
        private float _music;
        private float _sfx;

        private void OnEnable()
        {
            // Seeded before the listeners are attached, so it does not read as a change.
            SeedControls();
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            applyButton.onClick.AddListener(ApplySettings);
        }

        private void OnDisable()
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            applyButton.onClick.RemoveListener(ApplySettings);
            // Leaving the panel drops anything unapplied.
            SeedControls();
        }

        // Back to the saved values, which Apply has already updated if it was pressed.
        private void SeedControls()
        {
            _master = SoundSettings.Master;
            _music = SoundSettings.Music;
            _sfx = SoundSettings.Sfx;
            masterSlider.value = _master;
            musicSlider.value = _music;
            sfxSlider.value = _sfx;
        }

        // Held rather than written, so the level only moves on Apply.
        private void OnMasterChanged(float value) => _master = value;

        private void OnMusicChanged(float value) => _music = value;

        private void OnSfxChanged(float value) => _sfx = value;

        private void ApplySettings()
        {
            SoundSettings.Master = _master;
            SoundSettings.Music = _music;
            SoundSettings.Sfx = _sfx;
            SoundSettings.Save();
        }
    }
}