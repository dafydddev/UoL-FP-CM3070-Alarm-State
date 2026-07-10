using UnityEngine;

namespace Run
{
    // The runs the game offers: which difficulty profiles exist and how long a run can be.
    // The play menu presents these; it doesn't define them.
    [CreateAssetMenu(menuName = "Run/Run Options")]
    public class RunOptions : ScriptableObject
    {
        public RunDifficultyProfile[] profiles;
        public int[] runLengths; // e.g. 10, 20, 30
    }
}