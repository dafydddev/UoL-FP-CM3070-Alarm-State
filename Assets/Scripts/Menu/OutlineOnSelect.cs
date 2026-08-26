using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class OutlineOnSelect : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Color focusColour = new(0f, 0.78f, 0.91f, 1f);
        [SerializeField] private Outline outline;
        private Outline _outline;

        private void Awake()
        {
            // Get from child to support selectables which render their outline in a child (e.g. toggles).
            _outline = outline != null ? outline : GetComponentInChildren<Outline>(true);
            SetFocused(false);
        }

        private void OnEnable()
        {
            // Highlight the selected item if it's dynamically enabled / disabled.
            SetFocused(EventSystem.current.currentSelectedGameObject == gameObject);
        }

        private void OnDisable()
        {
            SetFocused(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetFocused(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetFocused(false);
        }

        // The outline stays on and is hidden by its alpha.
        private void SetFocused(bool focused)
        {
            if (!_outline) return;
            var colour = focusColour;
            colour.a = focused ? 1f : 0f;
            _outline.effectColor = colour;
        }
    }
}