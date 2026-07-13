using Generation.Tiles;
using Run;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    // The play panel: presents the game's run options and hands the player's
    // selection to the gameplay scene as the pending RunContext.
    public class PlayMenu : MonoBehaviour
    {
        // What the game offers; the menu only renders it.
        [SerializeField] private RunOptions options;

        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private TMP_Dropdown levelsDropdown;
        [SerializeField] private TMP_Dropdown layoutDropdown;
        [SerializeField] private Button startRunButton;

        // Player-facing names for the layout styles the game offers, in dropdown order.
        private static readonly (string label, TileLayoutStyle style)[] Layouts =
        {
            ("Direct", TileLayoutStyle.Spine),
            ("Winding", TileLayoutStyle.RandomWalk),
        };

        private void Start()
        {
            // One entry per difficulty profile, showing its label.
            difficultyDropdown.ClearOptions();
            foreach (var profile in options.profiles)
            {
                difficultyDropdown.options.Add(new TMP_Dropdown.OptionData(profile.label));
            }

            difficultyDropdown.RefreshShownValue();

            // One entry per run length.
            levelsDropdown.ClearOptions();
            foreach (var length in options.runLengths)
            {
                levelsDropdown.options.Add(new TMP_Dropdown.OptionData($"{length}"));
            }

            levelsDropdown.RefreshShownValue();

            // One entry per layout style.
            layoutDropdown.ClearOptions();
            foreach (var (label, _) in Layouts)
            {
                layoutDropdown.options.Add(new TMP_Dropdown.OptionData(label));
            }

            layoutDropdown.RefreshShownValue();
            startRunButton.onClick.AddListener(Play);
        }

        private void Play()
        {
            // Stash the selection for LevelOrchestrator, then enter the gameplay scene.
            RunContext.Pending = new RunContext(
                options.profiles[difficultyDropdown.value],
                1,
                options.runLengths[levelsDropdown.value],
                Layouts[layoutDropdown.value].style);
            SceneManager.LoadScene("Gameplay");
        }
    }
}