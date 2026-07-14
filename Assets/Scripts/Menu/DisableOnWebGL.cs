using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    [RequireComponent(typeof(Button))]
    public class DisableOnWebGL : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        GetComponent<Button>().interactable = false;
#endif
        }
    }
}