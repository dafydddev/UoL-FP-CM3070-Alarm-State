using UnityEngine;
using UnityEngine.InputSystem;

namespace Settings
{
    // Applies any binding overrides at startup, so the user doesn't need to open the settings menu.
    public class BindingLoader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset asset;

        private void Awake()
        {
            if (!asset) return;
            var json = BindingSettings.Overrides;
            if (!string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
        }
    }
}
