using Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class BackgroundScrollController : MonoBehaviour
    {
        [SerializeField] private Toggle scrollingToggle;
        [SerializeField] private Button applyButton;

        private bool _scrolling;

        private void OnEnable()
        {
            // Seeded before the listener is attached, so it does not read as a change.
            SeedControls();
            scrollingToggle.onValueChanged.AddListener(OnScrollingChanged);
            applyButton.onClick.AddListener(ApplySettings);
        }

        private void OnDisable()
        {
            scrollingToggle.onValueChanged.RemoveListener(OnScrollingChanged);
            applyButton.onClick.RemoveListener(ApplySettings);
            // Leaving the panel drops anything unapplied.
            SeedControls();
        }

        // Back to the saved value, which Apply has already updated if it was pressed.
        private void SeedControls()
        {
            _scrolling = BackgroundSettings.Scrolling;
            scrollingToggle.isOn = _scrolling;
        }

        // Held rather than written, so the backdrop keeps drifting until Apply.
        private void OnScrollingChanged(bool value) => _scrolling = value;

        private void ApplySettings()
        {
            BackgroundSettings.Scrolling = _scrolling;
            BackgroundSettings.Save();
        }
    }
}
