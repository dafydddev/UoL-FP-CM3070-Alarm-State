using UnityEngine;

namespace Player
{
    // The prompt above the player, shown while they stand on something they can use.
    public class UsePrompt : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer prompt;

        private bool _shown;

        private void Awake() => _shown = prompt.enabled;

        public void Show(bool shown)
        {
            if (shown == _shown) return;
            _shown = shown;
            prompt.enabled = shown;
        }
    }
}
