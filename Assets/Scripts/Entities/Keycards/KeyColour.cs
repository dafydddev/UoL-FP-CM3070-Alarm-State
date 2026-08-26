using Generation;
using UnityEngine;

namespace Entities.Keycards
{
    // Maps a key id to a stable, distinct colour so a keycard and the door it opens are shown in the same hue.
    public static class KeyColour
    {
        // Successive ordinals stepped by this land far apart on the wheel, so neighbouring keys never share a hue.
        private const float GoldenRatioConjugate = 0.6180339887f;

        // The seed is the graph's, so a level's palette varies while a key keeps its colour throughout that level.
        public static Color For(string keyId, int seed)
        {
            if (string.IsNullOrEmpty(keyId)) return Color.white;
            var offset = (Seeds.For(seed, Seeds.Keys) & 0x7fffffff) % 360 / 360f;
            var hue = (offset + Ordinal(keyId) * GoldenRatioConjugate) % 1f;
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }

        // A key's ordinal is the trailing number of its id.
        // An id with no trailing digits scores 0, so it takes the palette's first hue.
        private static int Ordinal(string keyId)
        {
            var start = keyId.Length;
            while (start > 0 && char.IsDigit(keyId[start - 1])) start--;
            return start < keyId.Length ? int.Parse(keyId[start..]) : 0;
        }
    }
}