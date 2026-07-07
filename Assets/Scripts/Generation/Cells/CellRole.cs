namespace Generation.Cells
{
    // The structural role a generated cell plays, before it is realised into a tile.
    // None is the default for an untouched cell (the void).
    public enum CellRole
    {
        None,
        Wall,
        Floor
    }
}