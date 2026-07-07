using UnityEngine;

namespace Entities.Keycards
{
    // Maps a key id to a stable, distinct colour so a keycard and the door it opens are shown in the same hue.
    public static class KeyColour
    {
        // Derives a colour from the key id's hash.
        // Same id always yields the same colour, empty/null ids default to white.
        public static Color For(string keyId)
        {
            if (string.IsNullOrEmpty(keyId)) return Color.white;
            // Hash -> positive value -> hue in [0,1); fixed saturation/value for a consistent look.
            var hue = (keyId.GetHashCode() & 0x7fffffff) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }
    }
}