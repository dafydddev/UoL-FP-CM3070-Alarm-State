using Entities;
using Player;
using Simulation;
using UnityEngine;

namespace Audio
{
    // The sounds that outlive the level's player prefab in the scene.
    [RequireComponent(typeof(AudioSource))]
    public class RunSfxController : MonoBehaviour
    {
        [SerializeField] private GameplaySfx death;
        [SerializeField] private GameplaySfx exitReached;
        [SerializeField] private GameplaySfx doorOpened;
        [SerializeField] private GameplaySfx alarmRaised;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            PlayerHealth.Died += OnDied;
            Exit.Reached += OnExitReached;
            LockedDoor.Opened += OnDoorOpened;
            AlarmState.ActiveChanged += OnAlarmChanged;
        }

        private void OnDisable()
        {
            PlayerHealth.Died -= OnDied;
            Exit.Reached -= OnExitReached;
            LockedDoor.Opened -= OnDoorOpened;
            AlarmState.ActiveChanged -= OnAlarmChanged;
        }

        private void OnDied() => Play(death);

        private void OnExitReached() => Play(exitReached);

        private void OnDoorOpened() => Play(doorOpened);

        // The event also carries the all-clear and the reset each level is built with; only the raise sounds.
        private void OnAlarmChanged(bool active)
        {
            if (active) Play(alarmRaised);
        }

        private void Play(GameplaySfx gameplaySfx)
        {
            if (gameplaySfx) gameplaySfx.Play(_source);
        }
    }
}
