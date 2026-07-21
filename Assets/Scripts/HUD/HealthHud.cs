using Player;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    // The strip of hearts on the HUD showing the player's remaining health.
    public class HealthHud : MonoBehaviour
    {
        // One image per possible heart, left to right; hearts beyond the player's maximum stay hidden.
        [SerializeField] private Image[] hearts;
        [SerializeField] private Sprite fullHeart;
        [SerializeField] private Sprite emptyHeart;

        private void OnEnable() => PlayerHealth.OnHealthChanged += Redraw;
        private void OnDisable() => PlayerHealth.OnHealthChanged -= Redraw;

        // Full hearts for the health remaining, empty for the hearts lost.
        private void Redraw(int current, int max)
        {
            for (var i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < current ? fullHeart : emptyHeart;
            }
        }
    }
}
