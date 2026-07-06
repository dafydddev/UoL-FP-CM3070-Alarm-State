using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation.Layout
{
    [CreateAssetMenu(menuName = "Generation/Tileset")]
    public class Tileset : ScriptableObject
    {
        [System.Serializable]
        private struct Entry
        {
            public CellRole role;
            public TileDefinition tile;
        }

        [SerializeField] private List<Entry> tiles = new();
        private Dictionary<CellRole, TileDefinition> _byRole;

        public TileDefinition For(CellRole role)
        {
            _byRole ??= tiles.ToDictionary(e => e.role, e => e.tile);
            return _byRole.GetValueOrDefault(role);
        }
    }
}