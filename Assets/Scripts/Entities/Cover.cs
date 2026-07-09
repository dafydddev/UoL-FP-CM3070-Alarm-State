using Player;
using UnityEngine;

namespace Entities
{
    // A hiding spot. While the player overlaps this trigger, they count as hidden
    // (via PlayerHiding's counter), letting them break the line of sight from guards.
    public class CoverItem : MonoBehaviour
    {
        private void Reset()
        {
        }

        // Player entered the hiding spot — mark them as in one more cover zone.

        // Player left the hiding spot — drop one cover zone.
        
   }
}