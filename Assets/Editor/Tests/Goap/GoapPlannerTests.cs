using System.Collections.Generic;
using Guards;
using Guards.Goap;
using NUnit.Framework;

namespace Editor.Tests.Goap
{
    // The planner's contract: a satisfied goal plans as an empty list and an unreachable one as null.
    // Where several action sequences reach the goal, the cheapest total cost wins.
    public class GoapPlannerTests
    {
        [Test]
        public void AnAlreadySatisfiedGoalPlansAsAnEmptyList()
        {
            var start = WorldState.Empty.With(Fact.PlayerCaught, true);
            var plan = GoapPlanner.Plan(start, WorldState.Empty.With(Fact.PlayerCaught, true), NoActions);

            Assert.That(plan, Is.Not.Null, "a satisfied goal must not plan as null");
            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void AnUnreachableGoalPlansAsNull()
        {
            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true), NoActions);

            Assert.That(plan, Is.Null);
        }

        [Test]
        public void ASingleActionReachesTheGoal()
        {
            var catchPlayer = Action("catch", WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true));
            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true),
                new List<GoapAction> { catchPlayer });

            Assert.That(plan, Is.EqualTo(new[] { catchPlayer }));
        }

        // An action whose preconditions are met only by another action must be planned behind it.
        [Test]
        public void PreconditionsAreChainedIntoThePlan()
        {
            var approach = Action("approach", WorldState.Empty, WorldState.Empty.With(Fact.AtPlayer, true));
            var catchPlayer = Action("catch", WorldState.Empty.With(Fact.AtPlayer, true),
                WorldState.Empty.With(Fact.PlayerCaught, true));

            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true),
                new List<GoapAction> { catchPlayer, approach });

            Assert.That(plan, Is.EqualTo(new[] { approach, catchPlayer }));
        }

        // Two routes reach the goal: one action costing 10, or two costing 1 each.
        [Test]
        public void TheCheapestRouteWinsEvenWhenItTakesMoreSteps()
        {
            var approach = Action("approach", WorldState.Empty, WorldState.Empty.With(Fact.AtPlayer, true));
            var catchPlayer = Action("catch", WorldState.Empty.With(Fact.AtPlayer, true),
                WorldState.Empty.With(Fact.PlayerCaught, true));
            var lunge = Action("lunge", WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true), cost: 10);

            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true),
                new List<GoapAction> { lunge, catchPlayer, approach });

            Assert.That(plan, Is.EqualTo(new[] { approach, catchPlayer }));
        }

        [Test]
        public void AnActionWhosePreconditionsCannotBeMetIsNeverPlanned()
        {
            var catchPlayer = Action("catch", WorldState.Empty.With(Fact.AtPlayer, true),
                WorldState.Empty.With(Fact.PlayerCaught, true));

            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true),
                new List<GoapAction> { catchPlayer });

            Assert.That(plan, Is.Null);
        }

        // An action that leaves the state unchanged must not be re-expanded forever.
        [Test]
        public void ActionsThatDoNotChangeTheStateTerminate()
        {
            var idle = Action("idle", WorldState.Empty, WorldState.Empty);
            var plan = GoapPlanner.Plan(WorldState.Empty, WorldState.Empty.With(Fact.PlayerCaught, true),
                new List<GoapAction> { idle });

            Assert.That(plan, Is.Null);
        }

        private static readonly IReadOnlyList<GoapAction> NoActions = new List<GoapAction>();

        private static GoapAction Action(string name, WorldState preconditions, WorldState effects, int cost = 1) =>
            new TestAction(name, preconditions, effects, cost);

        // Planning-only stand-in: the planner reads Preconditions, Effects and Cost, never Run.
        private sealed class TestAction : GoapAction
        {
            private readonly int _cost;
            private readonly string _name;

            public TestAction(string name, WorldState preconditions, WorldState effects, int cost)
            {
                _name = name;
                _cost = cost;
                Preconditions = preconditions;
                Effects = effects;
            }

            public override int Cost => _cost;
            public override ActionStatus Run(GuardAgent agent) => ActionStatus.Succeeded;
            public override string ToString() => _name;
        }
    }
}
