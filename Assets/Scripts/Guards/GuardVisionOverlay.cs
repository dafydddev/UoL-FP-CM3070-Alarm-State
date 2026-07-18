using System.Collections.Generic;
using UnityEngine;

namespace Guards
{
    // Paints the cells a guard can currently see straight into the game view (and builds),
    // so its field of view — range, facing arc, and how walls clip line of sight — is visible while playing.
    // Mirrors GuardDebugLabel: a child object drawn over the facility, rebuilt each frame from the guard's own senses.
    [RequireComponent(typeof(GuardAgent))]
    public class GuardVisionOverlay : MonoBehaviour
    {
        [SerializeField] private bool show = true;
        [SerializeField] private Color calmColor = new(1f, 0.95f, 0.4f, 0.16f); // patrolling
        [SerializeField] private Color alertColor = new(1f, 0.25f, 0.2f, 0.28f); // player in sight
        [SerializeField, Range(0.1f, 1f)] private float cellFill = 0.9f; // marker size vs the tile
        [SerializeField] private int sortingOrder = 1; // over the floor, under the guard sprite

        private GuardAgent _agent;
        private Transform _surface;
        private MeshRenderer _renderer;
        private Material _material;
        private Mesh _mesh;

        // Reused every frame so a rebuild allocates nothing.
        private readonly List<Vector3> _centers = new();
        private readonly List<Vector3> _verts = new();
        private readonly List<int> _tris = new();

        private void Awake()
        {
            _agent = GetComponent<GuardAgent>();

            var go = new GameObject("VisionOverlay");
            _surface = go.transform;
            _surface.SetParent(transform, false);

            _mesh = new Mesh { name = "GuardVision" };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().mesh = _mesh;

            // Sprites/Default is an always-included unlit shader that respects sorting order
            // and premultiplies alpha, so a translucent _Color blends cleanly over the tiles.
            _material = new Material(Shader.Find("Sprites/Default"));
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.sortingOrder = sortingOrder;
        }

        private void LateUpdate()
        {
            _renderer.enabled = show;
            if (!show) return;

            _agent.CollectVisibleCells(_centers, out var cellSize);
            Rebuild(cellSize * (0.5f * cellFill));
            _material.color = _agent.Memory.SeesPlayer ? alertColor : calmColor;
        }

        // One camera-facing quad per visible cell, expressed in the surface's local space,
        // so the guard's movement doesn't drag the overlay off the grid.
        private void Rebuild(Vector2 half)
        {
            _verts.Clear();
            _tris.Clear();

            foreach (var center in _centers)
            {
                var c = _surface.InverseTransformPoint(center);
                var i = _verts.Count;
                _verts.Add(c + new Vector3(-half.x, -half.y));
                _verts.Add(c + new Vector3(-half.x, half.y));
                _verts.Add(c + new Vector3(half.x, half.y));
                _verts.Add(c + new Vector3(half.x, -half.y));
                _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
                _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 3);
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetTriangles(_tris, 0);
        }

        private void OnDestroy()
        {
            if (_mesh) Destroy(_mesh);
            if (_material) Destroy(_material);
        }
    }
}
