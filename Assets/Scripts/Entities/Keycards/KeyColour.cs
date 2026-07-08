using Generation;
using UnityEngine;

namespace Entities.Keycards
{
    // Maps a key id to a stable, distinct colour so a keycard and the door it opens are shown in the same hue.
    public static class KeyColour
    {
        // Derives a colour from the key id's hash, mixed with the level seed so similar ids
        // still land on well-separated hues.
        // Same id and seed always yield the same colour, empty/null ids default to white.
        public static Color For(string keyId, int seed)
        {
            if (string.IsNullOrEmpty(keyId)) return Color.white;
            var hue = (Seeds.For(seed, Seeds.Keys, keyId.GetHashCode()) & 0x7fffffff) % 360 / 360f;
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }
    }
}
