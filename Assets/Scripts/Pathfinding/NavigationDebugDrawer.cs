using Generation.Facility;
using Simulation;
using UnityEngine;

namespace Pathfinding
{
    // Editor gizmo helper. This visualises the navigation grid and a test path between two transforms.
    public class NavigationDebugDrawer : MonoBehaviour
    {
        [Header("Facility")] [SerializeField] private FacilityOrchestrator orchestrator;

        [Header("Transform Points")] 
        [SerializeField] private Transform from; // start point of the test path
        [SerializeField] private Transform to; // end point of the test path

        [Header("Gizmo Colours")] [SerializeField]
        private Color walkableColor = Color.bisque;

        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color waypointColor = Color.green;

        [Header("Gizmo Sizes")] 
        [SerializeField] private float waypointRadius = 0.12f;
        [SerializeField] private float walkableWidth = 0.2f;

        [Header("Draw Flags")]
        [SerializeField] private bool drawWalkable = true; // whether to draw a marker on every walkable cell
        [SerializeField] private bool drawPath = true;

        // Called by the editor to draw gizmos while this object is selected.
        private void OnDrawGizmosSelected()
        {
            // Nothing to draw until a level has been generated.
            var world = orchestrator ? orchestrator.World : null;
            if (world == null) return;

            // Walkability is per-mover: judge it as the from-actor when one is
            // assigned, else as an anonymous keyless mover (locked doors block).
            var mover = from ? from.GetComponentInParent<Actor>() : null;

            // Draw a small cube on each cell the mover could stand on.
            if (drawWalkable)
            {
                Gizmos.color = walkableColor;
                for (var x = 0; x < world.Grid.Width; x++)
                {
                    for (var y = 0; y < world.Grid.Height; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!world.Navigator.Pathfinder.IsWalkable(cell, mover)) continue;
                        Gizmos.DrawCube(world.Navigator.CellToWorld(cell), Vector3.one * walkableWidth);
                    }
                }
            }

            // The path preview needs both endpoints assigned.
            if (!from || !to) return;

            // Ask the pathfinder for a route; bail if there isn't one.
            var path = world.Navigator.FindWorldPath(from.position, to.position, mover);
            if (path == null) return;
            if (!drawPath) return;
            // Draw the path as line segments between consecutive waypoints.
            Gizmos.color = pathColor;
            for (var i = 0; i < path.Count - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }

            // Mark each waypoint with a sphere.
            Gizmos.color = waypointColor;
            foreach (var p in path)
            {
                Gizmos.DrawSphere(p, waypointRadius);
            }
        }
    }
}