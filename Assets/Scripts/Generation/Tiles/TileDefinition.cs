using Generation.Cells;
using Simulation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Tiles
{
    // A kind of terrain: its visual and the rules its cells obey.
    [CreateAssetMenu(menuName = "Generation/Tile Definition")]
    public class TileDefinition : ScriptableObject, IEntryBlocker, IEnterHandler
    {
        [SerializeField] private TileBase tileBase;
        [SerializeField] private bool walkable;

        public TileBase TileBase => tileBase;

        public bool BlocksEntry(Actor mover) => !walkable;
        public void OnEntered(Actor mover) { }
    }
}