using Entities;
using Guards;
using Player;
using Settings;
using Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Haptics
{
    // Buzzes the player's gamepad on gameplay events.
    public class RumbleController : MonoBehaviour
    {
        [SerializeField] private InputDeviceState deviceState;
        [SerializeField] private Rumble hurt;
        [SerializeField] private Rumble death;
        [SerializeField] private Rumble alarmRaised;
        [SerializeField] private Rumble alarmDisabled;
        [SerializeField] private Rumble playerSpotted;
        [SerializeField] private Rumble doorOpened;
        [SerializeField] private Rumble enterCover;

        private Gamepad _pad;
        private float _stopAt;

        private void OnEnable()
        {
            PlayerHealth.Damaged += OnDamaged;
            PlayerHealth.Died += OnDied;
            AlarmState.ActiveChanged += OnAlarmChanged;
            GuardAgent.PlayerSpotted += OnPlayerSpotted;
            LockedDoor.Opened += OnDoorOpened;
            PlayerHiding.OnHiddenChanged += OnHiddenChanged;
        }

        private void OnDisable()
        {
            PlayerHealth.Damaged -= OnDamaged;
            PlayerHealth.Died -= OnDied;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            GuardAgent.PlayerSpotted -= OnPlayerSpotted;
            LockedDoor.Opened -= OnDoorOpened;
            PlayerHiding.OnHiddenChanged -= OnHiddenChanged;
            Stop();
        }

        // Motors hold their speed until told otherwise.
        private void Update()
        {
            if (_pad != null && Time.unscaledTime >= _stopAt) Stop();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Stop();
        }

        private void OnDamaged() => Play(hurt);

        private void OnDied() => Play(death);

        private void OnPlayerSpotted() => Play(playerSpotted);

        private void OnDoorOpened() => Play(doorOpened);

        private void OnAlarmChanged(bool active) => Play(active ? alarmRaised : alarmDisabled);

        private void OnHiddenChanged(bool hidden)
        {
            if (hidden) Play(enterCover);
        }

        private void Play(Rumble rumble)
        {
            if (!rumble || !RumbleSettings.Enabled) return;
            // Only buzz the pad the player is actually holding.
            if (!deviceState || deviceState.CurrentDevice is not Gamepad pad) return;
            if (_pad != null && _pad != pad) Stop();
            _pad = pad;
            _pad.SetMotorSpeeds(rumble.Low, rumble.High);
            _stopAt = Time.unscaledTime + rumble.Duration;
        }

        private void Stop()
        {
            _pad?.SetMotorSpeeds(0f, 0f);
            _pad = null;
        }
    }
}