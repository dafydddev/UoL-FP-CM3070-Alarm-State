using System.Collections;
using UnityEngine;

namespace Effects
{
    // Animates the iris material's _Radius to wipe the screen black and back.
    // In the shader graph 0 fully reveals the scene and coveredRadius fully blacks it out,
    // so Open() drives _Radius to 0 and Close() drives it to coveredRadius.
    public class ScreenWipe : MonoBehaviour
    {
        // The IrisMat asset used by the Full Screen Pass feature.
        [SerializeField] private Material irisMaterial;

        // How long a close or open takes.
        [SerializeField] private float duration = 0.4f;

        // The _Radius value at which the screen is fully black. 
        [SerializeField] private float coveredRadius = 1.2f;

        // Cached shader property id for the iris radius.
        private static readonly int Radius = Shader.PropertyToID("_Radius");

        // Start fully covered so the first level builds behind black rather than on-screen,
        private void Awake()
        {
            if (irisMaterial) irisMaterial.SetFloat(Radius, coveredRadius);
        }

        // Wipe to black: 0 -> coveredRadius.
        public IEnumerator Close() => To(coveredRadius);

        // Reveal the level: coveredRadius -> 0.
        public IEnumerator Open() => To(0f);

        // Lerp _Radius from its current value to target over duration. Unscaled
        // time so the wipe still runs if Time.timeScale is frozen (death/pause).
        private IEnumerator To(float target)
        {
            if (!irisMaterial) yield break; // a broken wipe must never hold the game up
            var from = irisMaterial.GetFloat(Radius);
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                irisMaterial.SetFloat(Radius, Mathf.Lerp(from, target, t / duration));
                yield return null;
            }

            irisMaterial.SetFloat(Radius, target);
        }
    }
}