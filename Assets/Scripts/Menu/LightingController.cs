using Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class LightingController : MonoBehaviour
    {
        [SerializeField] private Toggle lightingToggle;
        [SerializeField] private Button applyButton;

        private bool _lighting;

        private void OnEnable()
        {
            // Seeded before the listener is attached, so it does not read as a change.
            SeedControls();
            lightingToggle.onValueChanged.AddListener(OnHighContrastChanged);
            applyButton.onClick.AddListener(ApplySettings);
            SetApplyInteractable();
        }

        private void OnDisable()
        {
            lightingToggle.onValueChanged.RemoveListener(OnHighContrastChanged);
            applyButton.onClick.RemoveListener(ApplySettings);
            // Leaving the panel drops anything unapplied.
            SeedControls();
        }

        // Back to the saved value, which Apply has already updated if it was pressed.
        private void SeedControls()
        {
            _lighting = LightSettings.Lighting;
            lightingToggle.isOn = _lighting;
        }

        // Held rather than written, so the lighting only changes on Apply.
        private void OnHighContrastChanged(bool value)
        {
            _lighting = value;
            SetApplyInteractable();
        }

        private void ApplySettings()
        {
            LightSettings.Lighting = _lighting;
            LightSettings.Save();
            var events = EventSystem.current;
            var hadFocus = events && events.currentSelectedGameObject == applyButton.gameObject;
            SetApplyInteractable();
            // Dimming the button under the focus drops it, so the focus is put back.
            if (!hadFocus || events.currentSelectedGameObject) return;
            events.SetSelectedGameObject(applyButton.gameObject);
        }

        private void SetApplyInteractable()
        {
            applyButton.interactable = _lighting != LightSettings.Lighting;
        }
    }
}
