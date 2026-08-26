using UnityEngine;

namespace Spawners
{
    // Shared base: owns the spawned children's lifecycle.
    // The Spawn contract lives on:
    // - PropSpawner (set dressing, needs only the tilemap)
    // - EntitySpawner (sim participants, need the world context).
    public abstract class Spawner : MonoBehaviour
    {
        // Backwards, as Destroy leaves the child list intact this frame, but DestroyImmediate re-indexes it.
        public void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }
}