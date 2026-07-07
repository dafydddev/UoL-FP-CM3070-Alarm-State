using Player;
using UnityEngine;

namespace Entities
{
    // A hiding spot. While the player overlaps this trigger, they count as hidden
    // (via PlayerHiding's counter), letting them break the line of sight from guards.
    [RequireComponent(typeof(Collider2D))]
    public class CoverItem : MonoBehaviour
    {
        // When first added in the editor, default the collider to a trigger.
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        // Player entered the hiding spot — mark them as in one more cover zone.
        private void OnTriggerEnter2D(Collider2D other)
        {
            var hider = other.GetComponent<PlayerHiding>();
            if (hider) hider.Enter(this);
        }

        // Player left the hiding spot — drop one cover zone.
        private void OnTriggerExit2D(Collider2D other)
        {
            var hider = other.GetComponent<PlayerHiding>();
            if (hider) hider.Exit(this);
        }
    }
}