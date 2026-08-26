using UnityEngine;
using UnityEngine.EventSystems;

namespace Audio
{
    // The sounds selectables make when taking focus and being activated.
    public class SelectableSfx : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private UISfxController uiSfxController;

        // Stops the sounds from playing when Unity automatically sets focus when menus are opened.
        // Spent by the first select or click to arrive, whichever that turns out to be.
        private bool _consumeNext;

        public void OnSelect(BaseEventData eventData)
        {
            if (_consumeNext)
            {
                _consumeNext = false;
                return;
            }

            // A pointer selects and clicks in the one go, and the click reports it, so it is left to do so alone.
            if (eventData is PointerEventData) return;
            if (uiSfxController) uiSfxController.PlaySelect();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (uiSfxController) uiSfxController.PlaySubmit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_consumeNext)
            {
                _consumeNext = false;
                return;
            }

            if (uiSfxController) uiSfxController.PlaySubmit();
        }

        public void ConsumeNextSelect() => _consumeNext = true;
    }
}