using System.Collections.Generic;

namespace Generation
{
    // Fisher-Yates over a caller-supplied seeded RNG, shared by the generators that need a varied but reproducible order.
    // Draws rng.Next(i + 1) with i descending, so a given seed yields the order it always has.
    public static class Shuffle
    {
        // Shuffles in place; arrays and lists both satisfy IList<T>.
        public static void InPlace<T>(IList<T> items, System.Random rng)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        // Shuffles a copy, for the fixed direction tables that must stay in their declared order.
        public static T[] Copy<T>(T[] source, System.Random rng)
        {
            var copy = (T[])source.Clone();
            InPlace(copy, rng);
            return copy;
        }
    }
}