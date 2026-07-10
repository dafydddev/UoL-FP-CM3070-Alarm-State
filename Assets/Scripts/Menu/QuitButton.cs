using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Quits the game when its button is clicked; stops play mode in the editor instead.
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(Quit);

        private static void Quit()
        {
#if UNITY_EDITOR
            // Quit does nothing in the editor; stop play mode instead so the button is testable.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}