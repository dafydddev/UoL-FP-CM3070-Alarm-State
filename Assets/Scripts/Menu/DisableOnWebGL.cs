using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Disables a control the web build cannot honour, such as quitting or resizing the window.
    [RequireComponent(typeof(Selectable))]
    public class DisableOnWebGL : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        GetComponent<Selectable>().interactable = false;
#endif
        }
    }
}