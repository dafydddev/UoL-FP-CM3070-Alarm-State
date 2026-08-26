using Player;
using Simulation;
using UnityEngine;

namespace HUD
{
    // Drives the bar above the player showing how much of the worn disguise is left.
    // The bar is authored on the player: a track at full width with the fill drawn over it.
    [RequireComponent(typeof(PlayerDisguise))]
    public class DisguiseMeter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer track;
        [SerializeField] private SpriteRenderer fill;

        // How fast the drawn bar catches the level the disguise reports, in bar widths per second.
        [SerializeField, Min(0f)] private float fillSpeed = 2f;

        [SerializeField] private Color fullColour = Color.green;
        [SerializeField] private Color emptyColour = Color.red;

        // Must match the Pixel Perfect Camera's Assets PPU, as CameraFollow does.
        [SerializeField] private int pixelsPerUnit = 8;

        private PlayerDisguise _disguise;

        // The fraction currently drawn, chasing the disguise's own so the bar slides rather than jumps.
        private float _drawn;

        private void Awake() => _disguise = GetComponent<PlayerDisguise>();

        // Eases the drawn bar towards the level the disguise reports, then repaints it.
        // The bar holds still while anything freezes the game, the way the minimap blip holds its pulse.
        private void LateUpdate()
        {
            if (!track || !fill) return;

            var visible = _disguise.IsDisguised;
            track.enabled = visible;
            fill.enabled = visible;
            if (!visible)
            {
                _drawn = 0f; // the next disguise fills the bar from empty
                return;
            }

            if (!GameLock.Locked) _drawn = Mathf.MoveTowards(_drawn, _disguise.Remaining, fillSpeed * Time.deltaTime);

            // Whole pixels of fill, so the draining edge steps cleanly instead of shimmering.
            // The fill is drawn from its own left edge, so its sprite must be authored with a left pivot.
            var width = Mathf.Round(track.size.x * _drawn * pixelsPerUnit) / pixelsPerUnit;
            fill.size = new Vector2(width, fill.size.y);
            fill.color = Color.Lerp(emptyColour, fullColour, _drawn);
        }
    }
}