using Settings;
using UnityEngine;
using UnityEngine.EventSystems;
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
            SetApplyInteractable();
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
        private void OnScrollingChanged(bool value)
        {
            _scrolling = value;
            SetApplyInteractable();
        }
        

        private void ApplySettings()
        {
            BackgroundSettings.Scrolling = _scrolling;
            BackgroundSettings.Save();
            var events = EventSystem.current;
            var hadFocus = events && events.currentSelectedGameObject == applyButton.gameObject;
            SetApplyInteractable();
            if (!hadFocus || events.currentSelectedGameObject) return;
            events.SetSelectedGameObject(applyButton.gameObject);
        }

        private void SetApplyInteractable()
        {
            applyButton.interactable = _scrolling != BackgroundSettings.Scrolling;
        }
    }
}
