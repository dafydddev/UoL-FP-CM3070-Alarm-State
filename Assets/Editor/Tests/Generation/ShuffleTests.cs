using System;
using System.Collections.Generic;
using System.Linq;
using Generation;
using NUnit.Framework;

namespace Editor.Tests.Generation
{
    public class ShuffleTests
    {
        private static int[] Sequence(int count) => Enumerable.Range(0, count).ToArray();

        [Test]
        public void TheSameSeedProducesTheSameOrder()
        {
            var first = Sequence(20);
            var second = Sequence(20);

            Shuffle.InPlace(first, new Random(4242));
            Shuffle.InPlace(second, new Random(4242));

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void DifferentSeedsProduceDifferentOrders()
        {
            var first = Sequence(20);
            var second = Sequence(20);

            Shuffle.InPlace(first, new Random(1));
            Shuffle.InPlace(second, new Random(2));

            CollectionAssert.AreNotEqual(first, second);
        }

        [Test]
        public void ShufflingIsAPermutation([Range(0, 8)] int count)
        {
            var items = Sequence(count);

            Shuffle.InPlace(items, new Random(7));

            CollectionAssert.AreEquivalent(Sequence(count), items);
        }

        [Test]
        public void AListShufflesTheSameWayAsAnArray()
        {
            var array = Sequence(12);
            var list = new List<int>(Sequence(12));

            Shuffle.InPlace(array, new Random(99));
            Shuffle.InPlace(list, new Random(99));

            CollectionAssert.AreEqual(array, list);
        }

        [Test]
        public void CopyShufflesWithoutTouchingTheSource()
        {
            var source = Sequence(12);

            var shuffled = Shuffle.Copy(source, new Random(5));

            CollectionAssert.AreEqual(Sequence(12), source);
            Assert.AreNotSame(source, shuffled);
            CollectionAssert.AreEquivalent(source, shuffled);
            CollectionAssert.AreNotEqual(source, shuffled, "Copy cloned without shuffling");
        }

        [Test]
        public void EveryOrderingOfThreeItemsIsReachable()
        {
            var seen = new HashSet<string>();
            for (var seed = 0; seed < 500; seed++)
            {
                var items = Sequence(3);
                Shuffle.InPlace(items, new Random(seed));
                seen.Add(string.Join("", items));
            }

            Assert.AreEqual(6, seen.Count, $"only reached {string.Join(" ", seen.OrderBy(order => order))}");
        }
    }
}