using UnityEngine;
using UnityEngine.EventSystems;

namespace Audio
{
    // The sounds selectables make when taking focus and being activated.
    public class SelectableSounds : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private MenuAudio menuAudio;

        public void OnSelect(BaseEventData eventData)
        {
            if (menuAudio) menuAudio.PlaySelect();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (menuAudio) menuAudio.PlaySubmit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (menuAudio) menuAudio.PlaySubmit();
        }
    }
}