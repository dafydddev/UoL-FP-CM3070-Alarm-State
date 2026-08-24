using UnityEngine;

namespace Player
{
    // The prompt above the player, shown while they stand on something they can use.
    public class UsePrompt : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer prompt;

        public void Show(bool shown) => prompt.enabled = shown;
    }
}
