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
            // The player did not choose this focus, so the select sound is spent before it can play
            var selectableAudio = firstSelectable.GetComponent<SelectableSfx>();
            if (selectableAudio) selectableAudio.ConsumeNextSelect();
            firstSelectable.Select();
        }
    }
}