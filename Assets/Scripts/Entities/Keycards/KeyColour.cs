using Generation;
using UnityEngine;

namespace Entities.Keycards
{
    // Maps a key id to a stable, distinct colour so a keycard and the door it opens are shown in the same hue.
    // Key ids carry their level ordinal ("room_key_0", "room_key_1";
    // each advances the hue by the golden-ratio conjugate, producing well-distributed colours,
    // without requiring knowledge of all keys in the level.
    // The seed rotates the whole wheel so different levels still vary.
    // Same id and seed always yield the same colour, empty/null ids default to white.
    public static class KeyColour
    {
        private const float GoldenRatioConjugate = 0.6180339887f;

        public static Color For(string keyId, int seed)
        {
            if (string.IsNullOrEmpty(keyId)) return Color.white;
            var offset = (Seeds.For(seed, Seeds.Keys) & 0x7fffffff) % 360 / 360f;
            var hue = (offset + Ordinal(keyId) * GoldenRatioConjugate) % 1f;
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }

        // A key's ordinal is the trailing number of its id.
        private static int Ordinal(string keyId)
        {
            var start = keyId.Length;
            while (start > 0 && char.IsDigit(keyId[start - 1])) start--;
            return start < keyId.Length ? int.Parse(keyId[start..]) : 0;
        }
    }
}
