using System.Collections.Generic;
using UnityEngine;

namespace Generation.Layout
{
    // Builds a generated TileType grid into the 3D scene by instantiating a prefab per cell
    // on the X/Z plane (Y is up), scaled by cellSize world units per grid cell. Every prefab,
    // regardless of type, is placed at a strict cellSize grid position -- the same grid the
    // floor uses -- so walls always meet floor edges and corners always join correctly.
    public class FacilityLayoutBuilder : MonoBehaviour
    {
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject doorPrefab;
        [SerializeField] private Transform root;

        private TileType[,] _lastGrid;

        // Clears any previously built layout and instantiates a prefab for every non-empty cell.
        public void Build(TileType[,] grid)
        {
            _lastGrid = grid;
            var parent = root ? root : transform;
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var w = grid.GetLength(0);
            var d = grid.GetLength(1);

            for (var x = 0; x < w; x++)
            for (var z = 0; z < d; z++)
            {
                var cell = grid[x, z];
                if (cell == TileType.Empty) continue;

                var cellCenter = new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
                var hasJunctionAxis = TryGetJunctionAxis(grid, x, z, w, d, out var junctionAxis);

                // Floor only goes where something actually walks: Floor cells themselves, and
                // Door cells (the one passable point in an otherwise solid shared wall). The
                // solid Wall portions of a wall shared between two rooms, and the corner/junction
                // cells where two rings meet, are impassable barriers, not floor -- they don't
                // need ground underneath any more than a regular single-room wall does.
                if (cell == TileType.Floor || cell == TileType.Door)
                    PlaceCentered(floorPrefab, cellCenter, Quaternion.identity, parent);

                if (cell != TileType.Wall && cell != TileType.Door) continue;

                var prefab = cell == TileType.Door ? doorPrefab : wallPrefab;
                var directions = OrthogonalFloorDirections(grid, x, z, w, d);

                if (directions.Count == 1)
                {
                    // A normal straight wall segment, edge-aligned to hug its floor.
                    var dir = directions[0];
                    var rotation = Quaternion.LookRotation(dir, Vector3.up);
                    PlaceAtCellEdge(prefab, cellCenter, dir, cellSize, rotation, parent);
                    continue;
                }

                if (directions.Count == 2)
                {
                    // Floor on both opposite sides: a wall/door shared between two rooms.
                    // Edge-snapping can only ever satisfy one of the two floors, leaving the
                    // other with a gap as wide as cellSize minus the wall's own depth. Centring
                    // splits any leftover gap evenly instead of dumping it all on one side,
                    // without requiring the mesh to be stretched to fill the cell. (Floor on two
                    // ADJACENT/perpendicular sides can't occur here -- the wall ring is always
                    // exactly 1 cell thick between exactly two rooms on opposite sides.)
                    // The piece's flat side must face ACROSS the gap between the two rooms (i.e.
                    // along the direction of the Floor pair itself), not along it -- so it's
                    // rotated 90 degrees from the Floor direction, the same way a single straight
                    // wall's facing is perpendicular to the run of wall cells beside it.
                    var acrossDir = directions[0];
                    var alongDir = new Vector3(-acrossDir.z, 0f, acrossDir.x);
                    PlaceCentered(prefab, cellCenter, Quaternion.LookRotation(alongDir, Vector3.up), parent);
                    continue;
                }

                // No orthogonal Floor neighbour of its own. An isolated room corner has no
                // adjacent shared wall either -- skip it, since the two straight runs either
                // side already reach that corner. A room-to-room junction cell has at least one
                // orthogonal neighbour that IS itself a shared wall (Floor on opposite sides) --
                // place one piece continuing that neighbour's axis. If two DIFFERENT axes are
                // found (a true crossing of two wall runs), only the first is rendered: two
                // full-cellSize pieces both centred on this one cell would cross through each
                // other's full length, which needs a dedicated corner/cross-shaped prefab to
                // avoid -- there isn't one yet, so this leaves a smaller gap on the second axis
                // rather than an overlapping cross.
                if (hasJunctionAxis)
                    PlaceCentered(prefab, cellCenter, Quaternion.LookRotation(junctionAxis, Vector3.up), parent);
            }
        }

        // The first wall axis (east-west or north-south) found among this cell's 4 orthogonal
        // neighbours that is itself a wall/door shared between two rooms. Used for junction cells
        // that have no Floor neighbour of their own.
        private static bool TryGetJunctionAxis(TileType[,] grid, int x, int z, int w, int d, out Vector3 axis)
        {
            return TryGetSharedWallAxis(grid, x, z + 1, w, d, out axis) ||
                   TryGetSharedWallAxis(grid, x, z - 1, w, d, out axis) ||
                   TryGetSharedWallAxis(grid, x + 1, z, w, d, out axis) ||
                   TryGetSharedWallAxis(grid, x - 1, z, w, d, out axis);
        }

        private static bool TryGetSharedWallAxis(TileType[,] grid, int x, int z, int w, int d, out Vector3 axis)
        {
            axis = Vector3.zero;
            if (x < 0 || z < 0 || x >= w || z >= d) return false;
            if (grid[x, z] != TileType.Wall && grid[x, z] != TileType.Door) return false;

            if (IsFloor(grid, x - 1, z, w, d) && IsFloor(grid, x + 1, z, w, d))
            {
                axis = Vector3.right;
                return true;
            }

            if (IsFloor(grid, x, z - 1, w, d) && IsFloor(grid, x, z + 1, w, d))
            {
                axis = Vector3.forward;
                return true;
            }

            return false;
        }

        // The set of cardinal directions (out of N/S/E/W) in which this cell has a Floor
        // neighbour. 0 entries: a corner (isolated or a room-to-room junction). 1 entry: a
        // straight wall. 2 entries: always opposite (a wall shared between two rooms) -- the
        // wall ring is always exactly 1 cell thick, so adjacent/perpendicular Floor pairs can't
        // occur on a single cell.
        private static List<Vector3> OrthogonalFloorDirections(TileType[,] grid, int x, int z, int w, int d)
        {
            var directions = new List<Vector3>(2);
            if (IsFloor(grid, x, z + 1, w, d)) directions.Add(Vector3.forward);
            if (IsFloor(grid, x, z - 1, w, d)) directions.Add(Vector3.back);
            if (IsFloor(grid, x + 1, z, w, d)) directions.Add(Vector3.right);
            if (IsFloor(grid, x - 1, z, w, d)) directions.Add(Vector3.left);
            return directions;
        }

        private static bool IsFloorOnBothSides(TileType[,] grid, int x, int z, int w, int d)
        {
            var xPinched = IsFloor(grid, x - 1, z, w, d) && IsFloor(grid, x + 1, z, w, d);
            var zPinched = IsFloor(grid, x, z - 1, w, d) && IsFloor(grid, x, z + 1, w, d);
            return xPinched || zPinched;
        }

        private static bool IsFloor(TileType[,] grid, int x, int z, int w, int d)
        {
            if (x < 0 || z < 0 || x >= w || z >= d) return false;
            return grid[x, z] == TileType.Floor;
        }

        // Instantiates a prefab centred on a target world position regardless of the prefab's
        // pivot: rotates it first, measures its actual world-space bounds, then shifts it so
        // those bounds sit centred there. Without this, an off-centre pivot would displace the
        // mesh once rotated, since rotation happens around whatever the prefab's own pivot is.
        private static void PlaceCentered(GameObject prefab, Vector3 targetCenter, Quaternion rotation, Transform parent)
        {
            var instance = Instantiate(prefab, Vector3.zero, rotation, parent);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                instance.transform.position = targetCenter;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var offset = targetCenter - bounds.center;
            offset.y = 0f;
            instance.transform.position += offset;
        }

        // Instantiates a wall/door prefab centred on the cell along its width (tangent to dir,
        // same as PlaceCentered), but positioned along dir (its facing/depth axis) so its near
        // face -- the one facing the room -- sits exactly on the cell's edge, i.e. flush against
        // the neighbouring floor tile. A wall's depth is typically much thinner than a full cell,
        // so centring it in the cell (like PlaceCentered does) leaves a visible gap to the floor;
        // this anchors it to the edge instead, regardless of how thin or thick it actually is.
        private static void PlaceAtCellEdge(GameObject prefab, Vector3 cellCenter, Vector3 dir, float cellSize,
            Quaternion rotation, Transform parent)
        {
            var instance = Instantiate(prefab, Vector3.zero, rotation, parent);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                instance.transform.position = cellCenter + dir * (cellSize * 0.5f);
                return;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // Centre on the cell for width (and Y), same as PlaceCentered, but drop any
            // correction along dir -- that axis is handled separately below.
            var offset = cellCenter - bounds.center;
            offset.y = 0f;
            offset -= dir * Vector3.Dot(offset, dir);

            var absDir = new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z));
            var extentAlongDir = Vector3.Dot(bounds.extents, absDir);
            var currentNearFace = Vector3.Dot(bounds.center, dir) + extentAlongDir;
            var targetNearFace = Vector3.Dot(cellCenter, dir) + cellSize * 0.5f;
            var dirShift = targetNearFace - currentNearFace;

            instance.transform.position += offset + dir * dirShift;
        }

        // Draws the raw abstract grid as flat coloured squares floating above the built scene,
        // so the exact TileType at every cell (and its neighbours) can be read directly from a
        // top-down view -- removing any ambiguity from 3D rotation, prefab shape, or camera angle
        // when diagnosing which cells are Wall/Floor/Door/Empty.
        private void OnDrawGizmos()
        {
            if (_lastGrid == null) return;

            var w = _lastGrid.GetLength(0);
            var d = _lastGrid.GetLength(1);
            for (var x = 0; x < w; x++)
            for (var z = 0; z < d; z++)
            {
                var cell = _lastGrid[x, z];
                if (cell == TileType.Empty) continue;

                Gizmos.color = cell switch
                {
                    TileType.Floor => new Color(0f, 0.6f, 0f, 0.6f),
                    TileType.Wall => new Color(0.5f, 0.5f, 0.5f, 0.9f),
                    TileType.Door => new Color(1f, 0.5f, 0f, 0.9f),
                    _ => Color.clear
                };

                var center = new Vector3((x + 0.5f) * cellSize, 3f, (z + 0.5f) * cellSize);
                Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f));
            }
        }
    }
}
