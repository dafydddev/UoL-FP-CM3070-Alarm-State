using Generation.Cells;
using UnityEngine;

namespace Generation.Tiles
{
    [CreateAssetMenu(menuName = "Generation/Tileset")]
    public class Tileset : ScriptableObject
    {
        [SerializeField] private TileDefinition wall;
        [SerializeField] private TileDefinition floor;

        public TileDefinition For(CellRole role) => role switch
        {
            CellRole.Wall => wall,
            CellRole.Floor => floor,
            _ => null, // None / void → no tile
        };
    }
}