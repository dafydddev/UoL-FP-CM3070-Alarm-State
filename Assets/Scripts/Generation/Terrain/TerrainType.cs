namespace Generation.Terrain
{
    // A kind of exterior terrain painted outside the facility footprint.
    // Ordered loosely by elevation: water sits lowest, rock highest.
    public enum TerrainType
    {
        Water,
        Grass,
        Trees,     // living trees
        DeadTrees, // bare, dead trees
        Rock
    }
}