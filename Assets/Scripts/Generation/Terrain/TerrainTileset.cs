using Generation.Tiles;
using UnityEngine;

namespace Generation.Terrain
{
    // Maps each exterior terrain type to the tile(s) that draw it.
    // Land types hold several variant tiles (one is chosen per cell from a seeded hash of its position)
    // Water holds one pre-oriented tile per shoreline case.
    // Water naming rule: the letters are the edges of the sprite with shoreline on them.
    [CreateAssetMenu(menuName = "Generation/Terrain Tileset")]
    public class TerrainTileset : ScriptableObject
    {
        [Header("Land variants — one is chosen per cell")]
        [SerializeField] private TileDefinition[] grass;
        [SerializeField] private TileDefinition[] trees;
        [SerializeField] private TileDefinition[] deadTrees;
        [SerializeField] private TileDefinition[] rock;

        [Header("Water — no shore")]
        [SerializeField] private TileDefinition shoreNone;

        [Header("Water — shore on one side")]
        [SerializeField] private TileDefinition shoreN;
        [SerializeField] private TileDefinition shoreE;
        [SerializeField] private TileDefinition shoreS;
        [SerializeField] private TileDefinition shoreW;

        [SerializeField] private TileDefinition shoreNE;
        [SerializeField] private TileDefinition shoreES;
        [SerializeField] private TileDefinition shoreSW;
        [SerializeField] private TileDefinition shoreNW;

        [Header("Water — shore on two opposite sides")]
        [SerializeField] private TileDefinition shoreNS;
        [SerializeField] private TileDefinition shoreEW;

        [Header("Water — shore on three sides")]
        [SerializeField] private TileDefinition shoreNES;
        [SerializeField] private TileDefinition shoreESW;
        [SerializeField] private TileDefinition shoreNSW;
        [SerializeField] private TileDefinition shoreNEW;

        [Header("Water — shore on all four sides")]
        [SerializeField] private TileDefinition shoreNESW;

        // The tile for a land cell of type at (x, y). The variant is a stable hash of the seed and position,
        // so terrain is repeatable across regenerations of the same seed.
        public TileDefinition For(TerrainType type, int seed, int x, int y)
        {
            var variants = type switch
            {
                TerrainType.Grass => grass,
                TerrainType.Trees => trees,
                TerrainType.DeadTrees => deadTrees,
                TerrainType.Rock => rock,
                _ => null,
            };

            if (variants == null || variants.Length == 0) return null;

            // Reuse seeds mixer for a stable, well-distributed pick rather than hand-rolling a second one.
            var pick = (uint)Seeds.For(seed, x, y) % (uint)variants.Length;
            return variants[pick];
        }

        // The tile for a water cell whose sides touching land are the shore.
        // Every one of the sixteen combinations has its own pre-oriented tile.
        public TileDefinition WaterTile(Shore shore) => shore switch
        {
            Shore.None => shoreNone,

            Shore.North => shoreN,
            Shore.East => shoreE,
            Shore.South => shoreS,
            Shore.West => shoreW,

            Shore.North | Shore.East => shoreNE,
            Shore.East | Shore.South => shoreES,
            Shore.South | Shore.West => shoreSW,
            Shore.North | Shore.West => shoreNW,

            Shore.North | Shore.South => shoreNS,
            Shore.East | Shore.West => shoreEW,

            Shore.North | Shore.East | Shore.South => shoreNES,
            Shore.East | Shore.South | Shore.West => shoreESW,
            Shore.North | Shore.South | Shore.West => shoreNSW,
            Shore.North | Shore.East | Shore.West => shoreNEW,

            Shore.North | Shore.East | Shore.South | Shore.West => shoreNESW,

            _ => null,
        };
    }
}
