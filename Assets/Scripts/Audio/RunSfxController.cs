using Entities;
using Entities.Objectives;
using Guards;
using Mini_Games;
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
        [SerializeField] private GameplaySfx playerSpotted;
        [SerializeField] private GameplaySfx objectiveStarted;
        [SerializeField] private GameplaySfx objectiveCompleted;
        [SerializeField] private GameplaySfx sequenceCorrect;
        [SerializeField] private GameplaySfx sequenceWrong;
        [SerializeField] private GameplaySfx tileRotated;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        private void OnEnable()
        {
            PlayerHealth.Died += OnDied;
            Exit.Reached += OnExitReached;
            LockedDoor.Opened += OnDoorOpened;
            AlarmState.ActiveChanged += OnAlarmChanged;
            GuardAgent.PlayerSpotted += OnPlayerSpotted;
            Objective.MiniGameRequested += OnObjectiveStarted;
            Objective.Complete += OnObjectiveCompleted;
            SequenceGameController.KeyEntered += OnSequenceKeyEntered;
            PipeGameController.TileRotated += OnTileRotated;
        }

        private void OnDisable()
        {
            PlayerHealth.Died -= OnDied;
            Exit.Reached -= OnExitReached;
            LockedDoor.Opened -= OnDoorOpened;
            AlarmState.ActiveChanged -= OnAlarmChanged;
            GuardAgent.PlayerSpotted -= OnPlayerSpotted;
            Objective.MiniGameRequested -= OnObjectiveStarted;
            Objective.Complete -= OnObjectiveCompleted;
            SequenceGameController.KeyEntered -= OnSequenceKeyEntered;
            PipeGameController.TileRotated -= OnTileRotated;
        }

        private void OnDied() => Play(death);

        private void OnExitReached() => Play(exitReached);

        private void OnDoorOpened() => Play(doorOpened);

        private void OnPlayerSpotted() => Play(playerSpotted);

        private void OnObjectiveStarted(Objective objective) => Play(objectiveStarted);

        private void OnObjectiveCompleted(Objective objective) => Play(objectiveCompleted);

        private void OnSequenceKeyEntered(bool correct) => Play(correct ? sequenceCorrect : sequenceWrong);

        private void OnTileRotated() => Play(tileRotated);

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
