using Player;

namespace Tutorials
{
    public static class TutorialTopics
    {
        public static TutorialTopic Topic(this ItemKind kind) => kind switch
        {
            ItemKind.Distraction => TutorialTopic.ItemDistraction,
            ItemKind.Disguise => TutorialTopic.ItemDisguise,
            ItemKind.LockPick => TutorialTopic.ItemLockPick,
            _ => TutorialTopic.ItemHealthPack,
        };
    }
}
