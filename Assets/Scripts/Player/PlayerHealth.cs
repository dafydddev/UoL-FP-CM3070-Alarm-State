using System;
using Guards;
using Simulation;
using UnityEngine;

namespace Player
{
    // The player's hearts. An arrest costs one; losing the last ends the run.
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHearts = 3;

        // How long an arrest leaves the player untouchable, so guards that stay
        // on top of them drain hearts at a pace rather than every arrest.
        [SerializeField, Min(0f)] private float iFrameSeconds = 3f;

        // Fires with the new count whenever the hearts change, including the refill on spawn.
        public static event Action<int, int> OnHealthChanged; // (current, max)

        private int Current { get; set; }

        // The hearts remaining.
        public int Hearts => Current;

        // The hearts a full bar holds.
        public int MaxHearts => maxHearts;

        // Alive while at least one heart remains.
        private bool IsAlive => Current > 0;

        private float _iFrames; // seconds of invulnerability remaining

        private void Awake()
        {
            Current = maxHearts;
            OnHealthChanged?.Invoke(Current, maxHearts);
        }

        private void OnEnable() => GuardAgent.PlayerCaught += OnPlayerCaught;
        private void OnDisable() => GuardAgent.PlayerCaught -= OnPlayerCaught;

        // Counts the iFrame window down only while the game runs,
        // so a pause or level build doesn't quietly spend it.
        private void Update()
        {
            if (GameLock.Locked) return;
            if (_iFrames > 0f) _iFrames -= Time.deltaTime;
        }

        // Restores hearts, never past the maximum.
        // Returns false when there is nothing to restore, so a pickup can decline to spend itself.
        public bool Heal(int hearts)
        {
            if (!IsAlive || Current >= maxHearts) return false;
            Current = Mathf.Min(Current + hearts, maxHearts);
            OnHealthChanged?.Invoke(Current, maxHearts);
            return true;
        }

        // Sets the starting hearts for a carried-over level, clamped to the maximum.
        public void SetHearts(int hearts)
        {
            Current = Mathf.Clamp(hearts, 0, maxHearts);
            OnHealthChanged?.Invoke(Current, maxHearts);
        }

        // An arrest inside the iFrame window costs nothing; the guards keep trying regardless.
        private void OnPlayerCaught()
        {
            if (!IsAlive || _iFrames > 0f) return;
            Current--;
            _iFrames = iFrameSeconds;
            OnHealthChanged?.Invoke(Current, maxHearts);
        }
    }
}
