namespace Generation.Tiles
{
    // Which placement strategy shapes the abstract cell layout.
    // Chosen once per run, so a whole run reads either as a straight line or as a wander.
    public enum TileLayoutStyle
    {
        Spine, // straight root-to-primary line, branches fanned out to either side
        RandomWalk, // seeded self-avoiding walk; the layout drifts instead of lining up
    }
}
