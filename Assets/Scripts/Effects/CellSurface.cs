using System.Collections.Generic;
using UnityEngine;

namespace Effects
{
    // One translucent mesh of camera-facing quads, one per painted cell.
    // Shared by the overlays that mark out cells: guard vision, laser beams.
    public sealed class CellSurface
    {
        private readonly Transform _root;
        private readonly MeshRenderer _renderer;
        private readonly Material _material;
        private readonly Mesh _mesh;
        private readonly List<Vector3> _verts = new();
        private readonly List<int> _tris = new();

        public CellSurface(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            _root = go.transform;
            _root.SetParent(parent, false);

            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().mesh = _mesh;

            // Sprites/Default is an always-included unlit shader that respects sorting order
            // and premultiplies alpha, so a translucent _Color blends cleanly over the tiles.
            _material = new Material(Shader.Find("Sprites/Default"));
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.sortingOrder = sortingOrder;
        }

        public void SetVisible(bool visible) => _renderer.enabled = visible;

        public void Rebuild(IEnumerable<Vector3> centers, Vector2 half, Color color)
        {
            _material.color = color;
            _verts.Clear();
            _tris.Clear();

            // Centres arrive in world space. The quads are built locally to their roots.
            foreach (var center in centers)
            {
                var c = _root.InverseTransformPoint(center);
                var i = _verts.Count;
                _verts.Add(c + new Vector3(-half.x, -half.y));
                _verts.Add(c + new Vector3(-half.x, half.y));
                _verts.Add(c + new Vector3(half.x, half.y));
                _verts.Add(c + new Vector3(half.x, -half.y));
                _tris.Add(i);
                _tris.Add(i + 1);
                _tris.Add(i + 2);
                _tris.Add(i);
                _tris.Add(i + 2);
                _tris.Add(i + 3);
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetTriangles(_tris, 0);
        }

        public void Dispose()
        {
            if (_mesh) Object.Destroy(_mesh);
            if (_material) Object.Destroy(_material);
        }
    }
}