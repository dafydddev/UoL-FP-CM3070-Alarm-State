using UnityEngine;
using UnityEngine.Tilemaps;

namespace HUD.MiniMap
{
    // Fits the minimap camera to the generated facility.
    // The camera, render texture, and UI are authored in the scene;
    // only the framing is computed at runtime because the facility size varies.
    public class MinimapHud : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera minimapCamera;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private float padding = 2f; // world-unit margin around the facility

        // Called once the level's tiles are laid, as the bounds are read from the tilemap itself.
        public void Fit()
        {
            if (!minimapCamera || !tilemap) return;

            tilemap.CompressBounds();
            var bounds = tilemap.localBounds;
            var centre = tilemap.transform.TransformPoint(bounds.center);

            minimapCamera.transform.position = new Vector3(centre.x, centre.y, minimapCamera.transform.position.z);
            // The wider of the two fits, so neither axis is cropped whatever the facility's shape.
            minimapCamera.orthographicSize = Mathf.Max(
                bounds.extents.y + padding,
                (bounds.extents.x + padding) / minimapCamera.aspect);
        }
    }
}