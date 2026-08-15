using UnityEngine;
using UnityEngine.EventSystems;

namespace Audio
{
    // The sounds selectables make when taking focus and being activated.
    public class SelectableAudio : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private UIAudioController uiAudioController;
        // Stops the sounds from playing when Unity automatically sets focus when menus are opened.
        private bool _consumeNext;
        
        public void OnSelect(BaseEventData eventData)
        {
            if (_consumeNext) { _consumeNext = false; return; }
            if (eventData is PointerEventData) return;
            if (uiAudioController) uiAudioController.PlaySelect();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (uiAudioController) uiAudioController.PlaySubmit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_consumeNext) { _consumeNext = false; return; }
            if (uiAudioController) uiAudioController.PlaySubmit();
        }
        
        public void ConsumeNextSelect() => _consumeNext = true;
    }
}