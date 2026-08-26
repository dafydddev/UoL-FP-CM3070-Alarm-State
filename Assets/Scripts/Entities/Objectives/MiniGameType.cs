namespace Entities.Objectives
{
    // Which minigame type an objective presents.
    public enum MiniGameType
    {
        Pipes, // the circuit puzzle (holds the game lock).
        Sequence, // the sequence order (takes only the keys, the game continues to tick).
    }
}