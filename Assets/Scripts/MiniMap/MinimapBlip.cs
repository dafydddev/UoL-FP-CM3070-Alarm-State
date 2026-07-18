using Simulation;
using UnityEngine;

namespace MiniMap
{
    // Keeps the player's minimap blip readable.
    // The minimap camera is zoomed to frame the whole facility (see MinimapFramer).
    // This scales the blip in step with that zoom so it holds a constant on-screen size.
    // Adds a gentle pulse so it stays easy to pick out.
    [RequireComponent(typeof(SpriteRenderer))]
    public class MinimapBlip : MonoBehaviour
    {
        // Blip world scale per unit of the minimap camera's orthographic size.
        // So the blip keeps a constant on-screen size, however far the camera zooms out to frame the facility.
        [SerializeField, Min(0f)] private float relativeSize = 0.13f;

        [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.3f;
        [SerializeField, Min(0f)] private float pulseSpeed = 3f;

        [SerializeField] private Color blipColour = Color.white;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.color = blipColour;
        }
        
        private UnityEngine.Camera _minimapCamera;
        private float _pulseTime;

        private void Update()
        {
            if (!_minimapCamera && !TryResolveCamera()) return;

            // Advance the pulse only while play is running, so it holds still on the pause screen.
            if (!GameLock.Locked) _pulseTime += Time.deltaTime;

            var baseScale = _minimapCamera.orthographicSize * relativeSize;
            var pulse = 1f + pulseAmplitude * Mathf.Sin(_pulseTime * pulseSpeed);
            var scale = baseScale * pulse;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // The minimap camera is the one where the culling mask renders this blip's layer (MiniMapBlip), the main camera excludes it.
        // Resolving it this way avoids a scene reference that a spawned prefab can't hold.
        private bool TryResolveCamera()
        {
            var layerBit = 1 << gameObject.layer;
            foreach (var cam in UnityEngine.Camera.allCameras)
            {
                if ((cam.cullingMask & layerBit) == 0) continue;
                _minimapCamera = cam;
                return true;
            }

            return false;
        }
    }
}