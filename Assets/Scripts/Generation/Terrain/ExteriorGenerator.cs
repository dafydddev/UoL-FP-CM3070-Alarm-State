using Generation.Cells;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Terrain
{
    // Generates PCG terrain around (and in the gaps of) the facility footprint and paints it onto
    // a background tilemap. Two independent Perlin fields — elevation and moisture — are sampled
    // per tile and mapped to a biome; facility cells (and a buffer around them) are left blank so
    // the facility tilemap shows through.
    //
    // Land biomes pick a seeded variant per cell. Water cells pick the pre-oriented tile matching
    // which of their four neighbours are land, so any water shape the noise produces draws with a
    // continuous shoreline.
    //
    // A generator component like MissionGenerator: its tunables and tileset live here, and the
    // orchestrator drives it with the run's seed for repeatable output.
    public class ExteriorGenerator : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;   // the background tilemap terrain is painted onto
        [SerializeField] private TerrainTileset tileset;

        [Header("Extent")]
        [Tooltip("How many tiles of terrain to paint around the facility's bounding box.")]
        [Min(0)] public int margin = 10;
        [Tooltip("Keep terrain this many tiles clear of the facility footprint.")]
        [Min(0)] public int buffer;

        [Header("Noise shape")]
        [Tooltip("Base noise frequency; smaller = larger, smoother features.")]
        public float frequency = 0.08f;
        [Tooltip("Layers of detail summed together (fractal Brownian motion).")]
        [Range(1, 6)] public int octaves = 4;
        [Tooltip("How quickly each successive octave loses influence.")]
        [Range(0f, 1f)] public float persistence = 0.5f;
        [Tooltip("How quickly each successive octave gains frequency.")]
        [Min(1f)] public float lacunarity = 2f;

        [Header("Biome thresholds — elevation (0..1)")]
        [Range(0f, 1f)] public float waterLevel = 0.36f; // below this is water
        [Range(0f, 1f)] public float rockLevel = 0.72f;  // above this is bare rock

        [Header("Biome thresholds — moisture (0..1)")]
        [Range(0f, 1f)] public float treesMoisture = 0.55f; // wetter than this grows living trees
        [Range(0f, 1f)] public float deadMoisture = 0.40f;  // drier than this leaves dead trees

        // Clears any terrain this component previously painted. Kept separate from Paint because
        // the terrain shares its tilemap with the facility, which is cleared in the same phase.
        public void Clear()
        {
            if (tilemap) tilemap.ClearAllTiles();
        }

        // Generates the terrain and paints it onto the tilemap behind the facility. roles is the
        // facility's structural grid; any non-None cell counts as "inside the facility". `seed`
        // and `level` drive both the noise and the per-cell variant choice, so the same run
        // repaints identically.
        public void Paint(CellRole[,] roles, int seed, int level)
        {
            if (!tilemap || !tileset) return;

            var terrain = Generate(roles, seed, level);
            var w = terrain.GetLength(0);
            var h = terrain.GetLength(1);

            for (var i = 0; i < w; i++)
            for (var j = 0; j < h; j++)
            {
                var type = terrain[i, j];
                if (!type.HasValue) continue; // facility cell — leave the background layer blank here

                // Undo the margin offset so terrain aligns with the facility grid at (0,0).
                var pos = new Vector3Int(i - margin, j - margin, 0);

                var def = type.Value == TerrainType.Water
                    ? tileset.WaterTile(Shoreline(terrain, i, j))
                    : tileset.For(type.Value, seed, pos.x, pos.y);

                if (def) tilemap.SetTile(pos, def.TileBase);
            }
        }

        // Builds the biome grid covering the facility's bounding box expanded by `margin`.
        // Indexed [i, j] where tile coord = (i - margin, j - margin); null entries are the
        // facility footprint.
        private TerrainType?[,] Generate(CellRole[,] roles, int seed, int level)
        {
            var gridW = roles.GetLength(0);
            var gridH = roles.GetLength(1);

            // A cell is off-limits to terrain if it (or a neighbour within buffer) belongs to the facility.
            var blocked = BuildBlockedMask(roles, buffer);

            // Derive stable, well-separated sample offsets for the two noise fields from the seed.
            var rng = new System.Random(Seeds.For(seed, Seeds.Terrain, level));
            var eOff = new Vector2(NextOffset(rng), NextOffset(rng));
            var mOff = new Vector2(NextOffset(rng), NextOffset(rng));

            var paddedW = gridW + margin * 2;
            var paddedH = gridH + margin * 2;
            var terrain = new TerrainType?[paddedW, paddedH];

            for (var i = 0; i < paddedW; i++)
            for (var j = 0; j < paddedH; j++)
            {
                var tx = i - margin;
                var ty = j - margin;

                // Inside the facility footprint (or its buffer)? Leave it to the facility tilemap.
                if (tx >= 0 && ty >= 0 && tx < gridW && ty < gridH && blocked[tx, ty])
                {
                    terrain[i, j] = null;
                    continue;
                }

                // Sample both fields at the true tile coordinate so terrain is seamless
                // regardless of margin, then pick a biome.
                var elevation = Fbm(tx + eOff.x, ty + eOff.y);
                var moisture = Fbm(tx + mOff.x, ty + mOff.y);
                terrain[i, j] = Classify(elevation, moisture);
            }

            return terrain;
        }

        // Maps an elevation/moisture pair to a biome using the configured thresholds. Low ground
        // is water and high ground is rock; the middle band splits by moisture into living trees
        // (wet), dead trees (dry), or grass in between.
        private TerrainType Classify(float elevation, float moisture)
        {
            if (elevation < waterLevel) return TerrainType.Water;
            if (elevation > rockLevel) return TerrainType.Rock;
            if (moisture > treesMoisture) return TerrainType.Trees;
            if (moisture < deadMoisture) return TerrainType.DeadTrees;
            return TerrainType.Grass;
        }

        // The sides of the water cell at (i, j) that touch land. Out-of-bounds and facility cells
        // count as land, so terrain gets a shoreline at the map edge and around the facility.
        private static Shore Shoreline(TerrainType?[,] grid, int i, int j)
        {
            var shore = Shore.None;
            if (!IsWater(grid, i, j + 1)) shore |= Shore.North;
            if (!IsWater(grid, i + 1, j)) shore |= Shore.East;
            if (!IsWater(grid, i, j - 1)) shore |= Shore.South;
            if (!IsWater(grid, i - 1, j)) shore |= Shore.West;
            return shore;
        }

        private static bool IsWater(TerrainType?[,] grid, int i, int j) =>
            i >= 0 && j >= 0 && i < grid.GetLength(0) && j < grid.GetLength(1) &&
            grid[i, j] == TerrainType.Water;

        // Fractal Brownian motion: sums octaves of Perlin noise, normalised back to ~[0, 1].
        private float Fbm(float x, float y)
        {
            float sum = 0f, amplitude = 1f, freq = frequency, range = 0f;
            for (var o = 0; o < octaves; o++)
            {
                // Mathf.PerlinNoise returns [0,1]; centre it so octaves can subtract as well as add.
                sum += (Mathf.PerlinNoise(x * freq, y * freq) - 0.5f) * amplitude;
                range += amplitude;
                amplitude *= persistence;
                freq *= lacunarity;
            }

            // Back into [0,1]. range guards against a divide-by-zero at extreme settings.
            return range > 0f ? Mathf.Clamp01(sum / range + 0.5f) : 0.5f;
        }

        // Dilates the facility footprint by `buffer` cells (Chebyshev) so terrain keeps its distance.
        private static bool[,] BuildBlockedMask(CellRole[,] roles, int buffer)
        {
            var w = roles.GetLength(0);
            var h = roles.GetLength(1);
            var blocked = new bool[w, h];

            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
            {
                if (roles[x, y] == CellRole.None) continue;

                // Mark this facility cell and everything within `buffer` of it.
                for (var dx = -buffer; dx <= buffer; dx++)
                for (var dy = -buffer; dy <= buffer; dy++)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h) blocked[nx, ny] = true;
                }
            }

            return blocked;
        }

        // A large random offset that de-correlates a noise field and hides Mathf.PerlinNoise's
        // integer-lattice symmetry (it returns 0.5 at whole coordinates).
        private static float NextOffset(System.Random rng) => (float)(rng.NextDouble() * 10000.0 - 5000.0);
    }
}
