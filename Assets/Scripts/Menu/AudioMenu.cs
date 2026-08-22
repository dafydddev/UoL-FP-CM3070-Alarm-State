using Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class AudioMenu : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Button applyButton;

        private float _master;
        private float _music;
        private float _sfx;
        private float _ui;

        private void OnEnable()
        {
            // Seeded before the listeners are attached, so it does not read as a change.
            SeedControls();
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            uiSlider.onValueChanged.AddListener(OnUiChanged);
            applyButton.onClick.AddListener(ApplySettings);
            SetApplyInteractable();
        }

        private void OnDisable()
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            uiSlider.onValueChanged.RemoveListener(OnUiChanged);
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
            _ui = SoundSettings.Ui;
            masterSlider.value = _master;
            musicSlider.value = _music;
            sfxSlider.value = _sfx;
            uiSlider.value = _ui;
        }

        // Held rather than written, so the level only moves on Apply.
        private void OnMasterChanged(float value)
        {
            _master = value;
            SetApplyInteractable();
        }

        private void OnMusicChanged(float value)
        {
            _music = value;
            SetApplyInteractable();
        }

        private void OnSfxChanged(float value)
        {
            _sfx = value;
            SetApplyInteractable();
        }
        
        private void OnUiChanged(float value)
        {
            _ui = value;
            SetApplyInteractable();
        }

        private void ApplySettings()
        {
            SoundSettings.Master = _master;
            SoundSettings.Music = _music;
            SoundSettings.Sfx = _sfx;
            SoundSettings.Ui = _ui;
            SoundSettings.Save();

            var events = EventSystem.current;
            var hadFocus = events && events.currentSelectedGameObject == applyButton.gameObject;
            SetApplyInteractable();
            if (!hadFocus || events.currentSelectedGameObject) return;
            events.SetSelectedGameObject(applyButton.gameObject);
        }

        private void SetApplyInteractable()
        {
            applyButton.interactable = !Mathf.Approximately(_master, SoundSettings.Master) ||
                                       !Mathf.Approximately(_music, SoundSettings.Music) ||
                                       !Mathf.Approximately(_sfx, SoundSettings.Sfx) ||
                                       !Mathf.Approximately(_ui, SoundSettings.Ui);
        }
    }
}