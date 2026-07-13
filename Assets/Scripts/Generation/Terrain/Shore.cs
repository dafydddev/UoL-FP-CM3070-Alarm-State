using System;

namespace Generation.Terrain
{
    // Which sides of a water cell touch land. Every combination has a matching pre-oriented
    // tile in the terrain tileset, so this mask alone picks the water sprite.
    [Flags]
    public enum Shore
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }
}
