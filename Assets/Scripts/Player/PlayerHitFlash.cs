using Simulation;
using UnityEngine;

namespace Player
{
    // Tints the sprite on a hit, then blinks it for as long as the iFrames last.
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHitFlash : MonoBehaviour
    {
        [SerializeField] private Color hitColour = Color.red;
        [SerializeField, Min(0f)] private float flashSeconds = 0.15f;
        [SerializeField, Min(0.01f)] private float blinkInterval = 0.1f;

        // Dimmed rather than hidden, so the player never loses track of where they are.
        [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.5f;

        private PlayerHealth _health;
        private SpriteRenderer _sprite;
        private float _flash; // seconds of tint remaining
        private float _blink; // seconds into the current iFrame window

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable() => PlayerHealth.Damaged += OnDamaged;
        private void OnDisable() => PlayerHealth.Damaged -= OnDamaged;

        private void OnDamaged() => _flash = flashSeconds;

        // Paused with the game, matching the iFrame countdown.
        private void Update()
        {
            if (GameLock.Locked || !_sprite) return;

            if (_flash > 0f) _flash -= Time.deltaTime;
            _blink = _health.Invulnerable ? _blink + Time.deltaTime : 0f;

            var colour = _flash > 0f ? hitColour : Color.white;
            var dimmed = _health.Invulnerable && (int)(_blink / blinkInterval) % 2 == 1;
            colour.a = dimmed ? blinkAlpha : 1f;
            _sprite.color = colour;
        }
    }
}
