using UnityEngine;

namespace Audio
{
    // A sound the gameplay makes, and the variations that stop it wearing thin.
    [CreateAssetMenu(menuName = "Audio/Gameplay Sound Effect")]
    public class GameplaySfx : ScriptableObject
    {
        // Picked from at random, so repeats do not machine-gun.
        [SerializeField] private AudioClip[] clips;

        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        // Jittered per play, for the same reason as the variations.
        [SerializeField] private Vector2 pitchRange = new(0.95f, 1.05f);

        // The minimum seconds between plays, however many callers ask at once.
        [SerializeField, Min(0f)] private float minInterval;

        private float _nextPlay; // when this sound may next sound

        // Runtime only, so a stale gate does not survive into the next play session.
        private void OnEnable() => _nextPlay = 0f;

        public void Play(AudioSource source)
        {
            if (!source || clips == null || clips.Length == 0) return;
            if (Time.time < _nextPlay) return;
            _nextPlay = Time.time + minInterval;
            var clip = clips[Random.Range(0, clips.Length)];
            if (!clip) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip, volume);
        }
    }
}