using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        // The object the camera follows.
        [SerializeField] private Transform target;

        // How quickly the camera catches up to the target.
        [SerializeField] private float speed = 10f;

        // How far back the camera sits from the target.
        [SerializeField] private float zOffset = 10f;

        // Must match the Pixel Perfect Camera's Assets PPU.
        [SerializeField] private int pixelsPerUnit = 16;

        private void Update()
        {
            // Do nothing if there's no target to follow.
            if (!target) return;
            transform.position = SnapToPixel(Vector3.Lerp(transform.position, target.position + Vector3.back * zOffset, speed * Time.deltaTime));
        }


        public void SetTarget(Transform targetTransform)
        {
            // Set which object the camera should follow.
            target = targetTransform;
            SnapToTarget();
        }

        // Jump straight to the target with no interpolation — used on (re)spawn
        // so the camera doesn't slide across the level to the new position.
        private void SnapToTarget()
        {
            if (!target) return;
            transform.position = target.position + Vector3.back * zOffset;
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