using System;

namespace Guards.Goap
{
    // An immutable set of boolean facts packed into two bitmasks: which facts are specified, and their values.
    // A full snapshot specifies every fact; preconditions, effects and goals are partial states that only mention the facts they care about.
    // Being a small value type, states compare, hash and copy without allocating, which keeps the planner's search cheap.
    public readonly struct WorldState : IEquatable<WorldState>
    {
        private readonly int _mask; // facts this state specifies
        private readonly int _values; // their values, meaningful only where _mask is set

        private WorldState(int mask, int values)
        {
            _mask = mask;
            _values = values & mask;
        }

        public static WorldState Empty => default;

        // Returns a copy with the given fact specified as value.
        public WorldState With(Fact fact, bool value)
        {
            var bit = 1 << (int)fact;
            return new WorldState(_mask | bit, value ? _values | bit : _values & ~bit);
        }

        // True if every fact the requirement specifies is specified here with the same value.
        public bool Satisfies(WorldState required) =>
            (required._mask & ~_mask) == 0 &&
            ((_values ^ required._values) & required._mask) == 0;

        // Overlays the effects' facts onto this state (how the planner simulates an action).
        public WorldState Apply(WorldState effects) =>
            new(_mask | effects._mask, (_values & ~effects._mask) | effects._values);

        public bool Equals(WorldState other) => _mask == other._mask && _values == other._values;
        public override bool Equals(object obj) => obj is WorldState other && Equals(other);
        public override int GetHashCode() => (_mask * 397) ^ _values;
    }
}
