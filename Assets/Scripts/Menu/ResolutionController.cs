using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Settings;

namespace Menu
{
    public class ResolutionController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Toggle fullscreenToggle;

        private Vector2Int _resolution;
        private bool _isFullscreen;

        private readonly Vector2Int[] _supportedResolutions =
        {
            new(854, 480),
            new(1280, 720),
            new(1920, 1080),
        };

        private void Awake()
        {
            LoadSettings();
            ApplySettings();
        }

        private void OnEnable()
        {
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            applyButton.onClick.AddListener(ApplySettings);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        private void OnDisable()
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            applyButton.onClick.RemoveListener(ApplySettings);
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        }

        private void LoadSettings()
        {
            var savedResolution = Mathf.Clamp(
                ResolutionSettings.ResolutionIndex,
                0,
                _supportedResolutions.Length - 1
            );

            var savedFullscreen = ResolutionSettings.Fullscreen;

            dropdown.value = savedResolution;
            fullscreenToggle.isOn = savedFullscreen;

            _resolution = _supportedResolutions[savedResolution];
            _isFullscreen = savedFullscreen;

            // Repair the saved value if it was invalid
            ResolutionSettings.ResolutionIndex = savedResolution;
        }
        private void OnDropdownChanged(int index)
        {
            _resolution = _supportedResolutions[index];
            ResolutionSettings.ResolutionIndex = index;
        }

        private void OnFullscreenChanged(bool value)
        {
            _isFullscreen = value;
            ResolutionSettings.Fullscreen = value;
        }

        private void ApplySettings()
        {
            Screen.SetResolution(
                _resolution.x,
                _resolution.y,
                _isFullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed
            );

            ResolutionSettings.Save();
        }
    }
}