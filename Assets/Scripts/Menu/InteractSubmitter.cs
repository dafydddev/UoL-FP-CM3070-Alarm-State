using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Menu
{
    // Lets the interact key press the focused selectable (e.g. menu buttons)
    public class InteractSubmitter : MonoBehaviour
    {
        [SerializeField] private InputActionReference interactAction;
        [SerializeField] private InputActionReference submitAction;

        private void OnEnable() => interactAction.action.Enable();
        private void OnDisable() => interactAction.action.Disable();

        private void Update()
        {
            if (!interactAction.action.WasPressedThisFrame()) return;
            // Controls shared with submit (gamepad south) are already sent by the UI module
            if (submitAction.action.WasPressedThisFrame()) return;
            // Right mouse is also interact, only keys and buttons press the focused buttons
            if (interactAction.action.activeControl?.device is Pointer) return;

            var eventSystem = EventSystem.current;
            var selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
            if (!selected || !selected.activeInHierarchy) return;
            ExecuteEvents.Execute(selected, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
        }
    }
}
