namespace Guards.Goap
{
    // The world facts the planner reasons about, one bit each in a WorldState.
    // Facts are observations (SeesPlayer, HasLead).
    // Activity markers that only become true as action effects during planning (OnPatrol, PlayerCaught).
    public enum Fact
    {
        SeesPlayer,
        AtPlayer,
        PlayerCaught,
        HasLead,
        OnPatrol,
        AlarmRaised,
        WantsToRaiseAlarm
    }
}