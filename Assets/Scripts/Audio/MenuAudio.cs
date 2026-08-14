using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class MenuAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip submitClip;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        public void PlaySelect() => _source.PlayOneShot(selectClip);

        public void PlaySubmit() => _source.PlayOneShot(submitClip);
    }
}