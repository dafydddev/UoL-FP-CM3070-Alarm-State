using UnityEngine;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class UISfxController : MonoBehaviour
    {
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip submitClip;
        [SerializeField] private AudioClip purchaseClip;

        private AudioSource _source;

        private void Awake() => _source = GetComponent<AudioSource>();

        public void PlaySelect()
        {
            if (!selectClip) return;
            _source.PlayOneShot(selectClip);
        }

        public void PlaySubmit()
        {
            if (!submitClip) return;
            _source.PlayOneShot(submitClip);
        }

        public void PlayPurchase()
        { 
            if (!purchaseClip) return;
            _source.PlayOneShot(purchaseClip);
        }
    }
}