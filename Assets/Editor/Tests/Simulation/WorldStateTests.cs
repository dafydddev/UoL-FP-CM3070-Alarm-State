using System;
using Guards.Goap;
using NUnit.Framework;

namespace Editor.Tests.Simulation
{
    public class WorldStateTests
    {
        [Test]
        public void AnUnspecifiedFactDoesNotSatisfyARequirementForFalse()
        {
            Assert.IsFalse(WorldState.Empty.Satisfies(WorldState.Empty.With(Fact.SeesPlayer, false)));
        }

        [Test]
        public void AnEmptyRequirementIsAlwaysSatisfied()
        {
            Assert.IsTrue(WorldState.Empty.Satisfies(WorldState.Empty));
            Assert.IsTrue(WorldState.Empty.With(Fact.SeesPlayer, true).Satisfies(WorldState.Empty));
        }

        [Test]
        public void AMatchingFactSatisfiesTheRequirement([Values(true, false)] bool value)
        {
            Assert.IsTrue(WorldState.Empty.With(Fact.SeesPlayer, value)
                .Satisfies(WorldState.Empty.With(Fact.SeesPlayer, value)));
        }

        [Test]
        public void AConflictingFactFailsTheRequirement([Values(true, false)] bool value)
        {
            Assert.IsFalse(WorldState.Empty.With(Fact.SeesPlayer, value)
                .Satisfies(WorldState.Empty.With(Fact.SeesPlayer, !value)));
        }

        [Test]
        public void FactsBeyondTheRequirementDoNotPreventSatisfaction()
        {
            var state = WorldState.Empty
                .With(Fact.SeesPlayer, true)
                .With(Fact.AlarmRaised, true)
                .With(Fact.HasLead, false);

            Assert.IsTrue(state.Satisfies(WorldState.Empty.With(Fact.SeesPlayer, true)));
        }

        [Test]
        public void WithOverwritesAFactAlreadySpecified()
        {
            var state = WorldState.Empty.With(Fact.SeesPlayer, true).With(Fact.SeesPlayer, false);

            Assert.IsTrue(state.Satisfies(WorldState.Empty.With(Fact.SeesPlayer, false)));
            Assert.IsFalse(state.Satisfies(WorldState.Empty.With(Fact.SeesPlayer, true)));
        }

        [Test]
        public void ApplyOverridesOverlappingFactsAndKeepsTheRest()
        {
            var state = WorldState.Empty.With(Fact.OnPatrol, true).With(Fact.SeesPlayer, false);
            var result = state.Apply(WorldState.Empty.With(Fact.SeesPlayer, true));

            Assert.IsTrue(result.Satisfies(WorldState.Empty.With(Fact.SeesPlayer, true)), "the effect should win");
            Assert.IsTrue(result.Satisfies(WorldState.Empty.With(Fact.OnPatrol, true)), "untouched facts should survive");
        }

        [Test]
        public void ApplySpecifiesFactsTheStateDidNotMention()
        {
            var result = WorldState.Empty.Apply(WorldState.Empty.With(Fact.PlayerCaught, true));

            Assert.IsTrue(result.Satisfies(WorldState.Empty.With(Fact.PlayerCaught, true)));
        }

        // GoapPlanner keys its best-cost table on WorldState, so equal states must also hash alike.
        [Test]
        public void StatesAreEqualRegardlessOfTheOrderFactsWereSet()
        {
            var a = WorldState.Empty.With(Fact.SeesPlayer, true).With(Fact.HasLead, false);
            var b = WorldState.Empty.With(Fact.HasLead, false).With(Fact.SeesPlayer, true);

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        // Facts are packed one bit each into an int, so a 33rd fact would silently alias onto another.
        [Test]
        public void EveryFactFitsInTheBitmask()
        {
            foreach (Fact fact in Enum.GetValues(typeof(Fact)))
                Assert.Less((int)fact, 32, $"{fact} is out of bitmask range");
        }
    }
}
