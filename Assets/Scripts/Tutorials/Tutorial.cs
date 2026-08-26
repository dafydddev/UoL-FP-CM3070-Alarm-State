using System;

namespace Tutorials
{
    public static class Tutorial
    {
        // Set by the controller in the gameplay scene. True once it has taken the request on.
        // A false return leaves onDismissed to be run here, so the handler must not run it itself.
        internal static Func<TutorialTopic, Action, bool> Handler;

        // Shows the tutorial topic the first time it comes up and never again.
        // onDismissed runs when the panel closes, or straight away when nothing is shown.
        public static void ShowOnce(TutorialTopic topic, Action onDismissed = null)
        {
            if (Handler != null && Handler(topic, onDismissed)) return;
            onDismissed?.Invoke();
        }
    }
}