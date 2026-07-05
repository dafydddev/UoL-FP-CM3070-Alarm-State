using UnityEngine;

namespace Generation.Objectives
{
    // Marks a world object as an objective with a given id.
    // Hides itself once the tracker reports that objective complete.
    public class Objective : MonoBehaviour
    {
        public string id;

        // Find the tracker and listen for objective changes.
        private void OnEnable()
        {
        }

        // Stop listening when disabled.
        private void OnDisable()
        {
        }

        // Deactivate this object once its objective has been completed.
        private void OnOnObjectiveChanged()
        {
        }
    }
}