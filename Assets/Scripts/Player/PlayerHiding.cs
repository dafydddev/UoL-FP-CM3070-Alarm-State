using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Player
{
    // Tracks whether the player is hidden, supporting overlapping cover zones via a counter,
    // so leaving one patch while still inside another keeps the player hidden.
    public class PlayerHiding : MonoBehaviour
    {
        // Fires only when the hidden state actually flips, not on every cover enter/exit.
        public static event Action<bool> OnHiddenChanged;

        // How many cover zones the player is currently inside.
        private int _covers;

        // Hidden as long as the player is inside at least one cover zone.
        private bool IsHidden => _covers > 0;

        // Called when entering a cover zone. (Argument is unused; kept for caller convenience.)
        public void Enter(Object _)
        {
            _covers++;
            OnHiddenChanged?.Invoke(IsHidden);
        }

        // Called when leaving a cover zone, clamped so the count never goes negative.
        public void Exit(Object _)
        {
            _covers = Mathf.Max(0, _covers - 1);
            OnHiddenChanged?.Invoke(IsHidden);
        }
    }
}