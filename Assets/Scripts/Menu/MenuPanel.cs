using Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class MenuPanel : MonoBehaviour
    {
        // The first selectable on the menu, setting focus onto it, helps with keyboard navigation
        [SerializeField] private Selectable firstSelectable;

        public void SetActive(bool isActive)
        {
            // Set the active state of the game object
            gameObject.SetActive(isActive);
            // If the game object is active and the first button is not null, set focus onto it
            if (!isActive || !firstSelectable) return;
            var selectableAudio = firstSelectable.GetComponent<SelectableAudio>();
            if (selectableAudio) selectableAudio.ConsumeNextSelect();
            firstSelectable.Select();
        }
    }
}