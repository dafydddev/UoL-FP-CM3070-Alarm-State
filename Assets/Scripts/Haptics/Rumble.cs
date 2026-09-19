using UnityEngine;

namespace Haptics
{
    // A buzz the gamepad makes, motor strengths, and how long it lasts.
    [CreateAssetMenu(menuName = "Haptics/Rumble")]
    public class Rumble : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float low = 0.5f;  // heavy motor
        [SerializeField, Range(0f, 1f)] private float high = 0.5f; // light motor
        [SerializeField, Min(0f)] private float duration = 0.15f;

        public float Low => low;
        public float High => high;
        public float Duration => duration;
    }
}
