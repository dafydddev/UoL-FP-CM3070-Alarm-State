using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // Takes focus when the pointer lands on this selectable.
    // Makes the mouse move the highlight just like the navigation keys, rather than leaving the two disagreeing.
    [RequireComponent(typeof(Selectable))]
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable _selectable;
        private void Awake() => _selectable = GetComponent<Selectable>();

        // Selecting from the pointer is what SelectableSfx's PointerEventData test is there to keep quiet.
        public void OnPointerEnter(PointerEventData eventData) => _selectable.Select();
    }
}