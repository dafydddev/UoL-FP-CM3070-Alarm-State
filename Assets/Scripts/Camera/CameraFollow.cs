using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        // The object the camera follows.
        private Transform _target;

        // How far back the camera sits from the target.
        [SerializeField] private float zOffset = 10f;

        // Must match the Pixel Perfect Camera's Assets PPU.
        [SerializeField] private int pixelsPerUnit = 8;

        private void LateUpdate()
        {
            if (!_target) return;
            transform.position = SnapToPixel(_target.position + Vector3.back * zOffset);
        }

        public void SetTarget(Transform targetTransform)
        {
            _target = targetTransform;
            SnapToTarget();
        }

        // Jump straight to the target — used on (re)spawn.
        private void SnapToTarget()
        {
            if (!_target) return;
            transform.position = SnapToPixel(_target.position + Vector3.back * zOffset);
        }

        // Round x/y to the nearest whole pixel so the camera never sits mid-pixel.
        private Vector3 SnapToPixel(Vector3 p)
        {
            var unit = 1f / pixelsPerUnit;
            return new Vector3(
                Mathf.Round(p.x / unit) * unit,
                Mathf.Round(p.y / unit) * unit,
                p.z);
        }
    }
}