using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Layout
{
    // A kind of tile: its visual and the rules its cells obey.
    [CreateAssetMenu(menuName = "Generation/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        [SerializeField] private TileBase tileBase;
        [SerializeField] private bool walkable;

        public TileBase TileBase => tileBase;
        public bool Walkable => walkable;
    }
}