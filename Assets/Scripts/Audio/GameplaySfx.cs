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

        // The minimum seconds between plays.
        [SerializeField, Min(0f)] private float minInterval;

        public float MinInterval => minInterval;

        public void Play(AudioSource source)
        {
            if (!source || clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (!clip) return;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip, volume);
        }
    }
}