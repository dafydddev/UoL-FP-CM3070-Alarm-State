using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform targetOverRide;
        // The object the camera follows.
        private Transform _target;

        // How far back the camera sits from the target.
        [SerializeField] private float zOffset = 10f;

        // Must match the scene's Pixel Perfect Camera's PPU.
        [SerializeField] private int pixelsPerUnit = 8;

        // Editor override wins over the runtime target.
        private Transform Target => targetOverRide ? targetOverRide : _target;

        private void LateUpdate() => SnapToTarget();

        public void SetTarget(Transform targetTransform)
        {
            _target = targetTransform;
            SnapToTarget();
        }

        // Jump straight to the target (e.g. to the player after a level transition).
        private void SnapToTarget()
        {
            if (!Target) return;
            transform.position = SnapToPixel(Target.position + Vector3.back * zOffset);
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