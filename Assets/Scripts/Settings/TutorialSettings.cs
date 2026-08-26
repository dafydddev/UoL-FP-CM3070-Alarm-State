using Tutorials;

namespace Settings
{
    // Which briefings the player has been shown, from the shared save profile.
    public static class TutorialSettings
    {
        // Claims a topic for its one showing. Records as it tests,
        // so a caller that declines the showing has still spent it; the topic never comes up again.
        public static bool TryMarkSeen(TutorialTopic topic)
        {
            if (SaveSystem.Data.seenTutorials.Contains(topic)) return false;
            SaveSystem.Data.seenTutorials.Add(topic);
            SaveSystem.Save();
            return true;
        }

#if UNITY_EDITOR
        // Debug: see the briefings again without wiping the profile.
        [UnityEditor.MenuItem("Tools/Reset Tutorials")]
        public static void Reset()
        {
            SaveSystem.Data.seenTutorials.Clear();
            SaveSystem.Save();
        }
#endif
    }
}