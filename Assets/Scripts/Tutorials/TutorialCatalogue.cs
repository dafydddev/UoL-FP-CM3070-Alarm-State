using System.Linq;
using UnityEngine;

namespace Tutorials
{
    // The tutorials that the gameplay scene can show, looked up by topic.
    [CreateAssetMenu(menuName = "Tutorials/Tutorial Catalogue")]
    public class TutorialCatalogue : ScriptableObject
    {
        [SerializeField] private TutorialEntry[] entries;

        // Null when a topic has no entry authored; it never shows and stays unseen.
        public TutorialEntry Find(TutorialTopic topic)
        {
            return entries?.FirstOrDefault(entry => entry && entry.topic == topic);
        }
    }
}