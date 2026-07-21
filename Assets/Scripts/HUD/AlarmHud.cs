using Simulation;
using UnityEngine;

namespace HUD
{
    // Shows an on-screen indicator while the alarm is sounding.
    public class AlarmHud : MonoBehaviour
    {
        // The indicator shown while the alarm is active.
        [SerializeField] private GameObject indicator;

        private void OnEnable()
        {
            AlarmState.ActiveChanged += Show;
            Show(false); // start clear until an alarm is raised
        }

        private void OnDisable() => AlarmState.ActiveChanged -= Show;

        private void Show(bool active)
        {
            if (indicator) indicator.SetActive(active);
        }
    }
}
