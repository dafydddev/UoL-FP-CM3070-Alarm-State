using System.Collections.Generic;
using Guards;
using Guards.Goap;
using NUnit.Framework;

namespace Editor.Tests.Goap
{
    // The planner: it finds the cheapest chain of actions to a goal, or reports that there is none.
    public class GoapPlannerTests
    {
        // Empty and null are distinct outcomes: nothing to do, against nothing that would do.
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

        // Passed out of order, so the chain is worked out by the planner.
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

        // The lunge reaches the goal in one step, but costs more than the two steps together.
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

        // An action that leaves the state as it found it would loop the search were the visited set not consulted.
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

        private sealed class TestAction : GoapAction
        {
            private readonly string _name;

            public TestAction(string name, WorldState preconditions, WorldState effects, int cost)
            {
                _name = name;
                Cost = cost;
                Preconditions = preconditions;
                Effects = effects;
            }

            public override int Cost { get; }

            public override ActionStatus Run(GuardAgent agent) => ActionStatus.Succeeded;
            public override string ToString() => _name;
        }
    }
}