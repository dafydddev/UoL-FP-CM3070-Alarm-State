using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Settings;
using UnityEngine.EventSystems;

namespace Menu
{
    public class ResolutionController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Toggle fullscreenToggle;

        // What the controls currently read, and what was last applied.
        private int _index;
        private bool _isFullscreen;
        private int _appliedIndex;
        private bool _appliedFullscreen;

        // The saved index points into this, so entries are append-only: reordering would remap saved settings.
        private readonly Vector2Int[] _supportedResolutions =
        {
            new(640, 360),
            new(854, 480),
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440),
        };

        private void Awake()
        {
            LoadSettings();
            ApplyResolution();
        }

        private void OnEnable()
        {
            // Seeded before the listeners are attached, so it does not read as a change.
            SeedControls();
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            applyButton.onClick.AddListener(ApplySettings);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            SetApplyInteractable();
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            applyButton.onClick.RemoveListener(ApplySettings);
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            // Leaving the panel drops anything unapplied.
            SeedControls();
        }

        private void LoadSettings()
        {
            _appliedIndex = Mathf.Clamp(
                ResolutionSettings.ResolutionIndex,
                0,
                _supportedResolutions.Length - 1
            );

            _appliedFullscreen = ResolutionSettings.Fullscreen;

            // Repair the saved value if it was invalid
            ResolutionSettings.ResolutionIndex = _appliedIndex;
        }

        // Puts the controls and the pending values back to what was last applied.
        private void SeedControls()
        {
            _index = _appliedIndex;
            _isFullscreen = _appliedFullscreen;
            dropdown.value = _appliedIndex;
            fullscreenToggle.isOn = _appliedFullscreen;
        }

        private void OnDropdownChanged(int index)
        {
            _index = index;
            SetApplyInteractable();
        }

        private void OnFullscreenChanged(bool value)
        {
            _isFullscreen = value;
            SetApplyInteractable();
        }


        private void ApplySettings()
        {
            _appliedIndex = _index;
            _appliedFullscreen = _isFullscreen;

            ResolutionSettings.ResolutionIndex = _appliedIndex;
            ResolutionSettings.Fullscreen = _appliedFullscreen;

            ApplyResolution();
            ResolutionSettings.Save();

            var events = EventSystem.current;
            var hadFocus = events && events.currentSelectedGameObject == applyButton.gameObject;
            SetApplyInteractable();
            // Dimming the button under the focus drops it, so the focus is put back.
            if (!hadFocus || events.currentSelectedGameObject) return;
            events.SetSelectedGameObject(applyButton.gameObject);
        }

        private void ApplyResolution()
        {
            // The page owns the canvas size on web.
#if !UNITY_WEBGL || UNITY_EDITOR
            var resolution = _supportedResolutions[_appliedIndex];

            Screen.SetResolution(
                resolution.x,
                resolution.y,
                _appliedFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed
            );
#endif
        }

        private void SetApplyInteractable()
        {
            applyButton.interactable = _index != _appliedIndex || _isFullscreen != _appliedFullscreen;
        }
    }
}