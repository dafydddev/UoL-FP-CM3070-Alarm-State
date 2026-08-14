using UnityEngine;
using UnityEngine.EventSystems;

namespace Audio
{
    // The sounds selectables make when taking focus and being activated.
    public class SelectableSounds : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private UIAudioController uiAudioController;

        public void OnSelect(BaseEventData eventData)
        {
            // avoid double playing the audio on pointer events
            if (eventData is PointerEventData) return;
            if (uiAudioController) uiAudioController.PlaySelect();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (uiAudioController) uiAudioController.PlaySubmit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (uiAudioController) uiAudioController.PlaySubmit();
        }
    }
}