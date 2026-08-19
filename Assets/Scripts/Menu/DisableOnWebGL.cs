using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
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