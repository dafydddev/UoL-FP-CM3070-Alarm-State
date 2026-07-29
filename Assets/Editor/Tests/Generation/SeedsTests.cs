using System.Linq;
using System.Reflection;
using Generation;
using NUnit.Framework;

namespace Editor.Tests.Generation
{
    public class SeedsTests
    {
        private static readonly (string Name, int Id)[] Subsystems = typeof(Seeds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(int))
            .Select(field => (field.Name, (int)field.GetRawConstantValue()))
            .ToArray();

        private static readonly int[] BaseSeeds = { 0, 1, -1, 12345, int.MaxValue };

        [Test]
        public void TheSubsystemIdsAreDistinct()
        {
            var shared = Subsystems
                .GroupBy(subsystem => subsystem.Id)
                .Where(group => group.Count() > 1)
                .Select(group => $"{string.Join(" and ", group.Select(s => s.Name))} both use id {group.Key}")
                .ToArray();

            Assert.That(shared, Is.Empty);
        }

        [Test]
        public void EverySubsystemGetsItsOwnSeed([ValueSource(nameof(BaseSeeds))] int seed)
        {
            var derived = Subsystems.Select(s => (s.Name, Seed: Seeds.For(seed, s.Id))).ToArray();

            foreach (var a in derived)
            foreach (var b in derived)
            {
                if (a.Name == b.Name) continue;
                Assert.AreNotEqual(a.Seed, b.Seed, $"{a.Name} and {b.Name} share a stream from base seed {seed}");
            }
        }

        [Test]
        public void ConsecutiveLevelsDoNotWalkTheSeedInStep([ValueSource(nameof(BaseSeeds))] int seed)
        {
            var perLevel = Enumerable.Range(1, 12)
                .Select(level => Seeds.For(seed, Seeds.Rooms, level))
                .ToArray();

            CollectionAssert.AllItemsAreUnique(perLevel);

            var ascending = perLevel.OrderBy(value => value).ToArray();
            Assert.That(perLevel, Is.Not.EqualTo(ascending),
                "the salt is tracking the seed rather than mixing into it");
            Assert.That(perLevel, Is.Not.EqualTo(ascending.Reverse().ToArray()));
        }

        [Test]
        public void DifferentBaseSeedsGiveDifferentStreams()
        {
            CollectionAssert.AllItemsAreUnique(BaseSeeds.Select(seed => Seeds.For(seed, Seeds.Rooms, 1)).ToArray());
        }
    }
}