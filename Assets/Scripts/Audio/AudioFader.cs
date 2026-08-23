using System.Collections;
using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioFader : MonoBehaviour
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private float maxVolume = 1f;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        public void FadeIn() => Fade(maxVolume);
        public void FadeOut() => Fade(0f);

        private void OnEnable()
        {
            _source.volume = 0f;
            FadeIn();
        }

        private void Fade(float target)
        { 
            StartCoroutine(FadeTo(target));
        }

        private IEnumerator FadeTo(float target)
        {
            var start = _source.volume;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                // Unscaled, so a paused timeScale does not stall the fade.
                elapsed += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            _source.volume = target;
        }
        
    }
}