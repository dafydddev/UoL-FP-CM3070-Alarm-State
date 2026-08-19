using UnityEngine;

namespace Tutorials
{
    [CreateAssetMenu(menuName = "Tutorials/Tutorial Entry")]
    public class TutorialEntry : ScriptableObject
    {
        private const int BodyMinLines = 3;
        private const int BodyMaxLines = 10;

        public TutorialTopic topic;

        public string title;

        [TextArea(BodyMinLines, BodyMaxLines)] public string body;

        // Optional; the panel hides its image without one.
        public Sprite image;
    }
}