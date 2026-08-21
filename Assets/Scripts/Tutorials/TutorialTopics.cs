using Player;

namespace Tutorials
{
    public static class TutorialTopics
    {
        public static TutorialTopic Topic(this ItemType type) => type switch
        {
            ItemType.Distraction => TutorialTopic.ItemDistraction,
            ItemType.Disguise => TutorialTopic.ItemDisguise,
            ItemType.LockPick => TutorialTopic.ItemLockPick,
            _ => TutorialTopic.ItemHealthPack,
        };
    }
}
