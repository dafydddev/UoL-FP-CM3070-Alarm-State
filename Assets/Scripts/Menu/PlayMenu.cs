using Generation.Tiles;
using Run;
using Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Menu
{
    // Presents the game's run options and hands the player's selection to the gameplay scene as the pending RunContext.
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

            // Opens on the last run's difficulty, else the default.
            var difficultyIndex = System.Array.FindIndex(options.profiles, p => p.label == RunSettings.Difficulty);
            if (difficultyIndex < 0) difficultyIndex = System.Array.IndexOf(options.profiles, options.defaultProfile);
            if (difficultyIndex >= 0) difficultyDropdown.SetValueWithoutNotify(difficultyIndex);

            difficultyDropdown.RefreshShownValue();

            // One entry per run length.
            levelsDropdown.ClearOptions();
            foreach (var length in options.runLengths)
            {
                levelsDropdown.options.Add(new TMP_Dropdown.OptionData($"{length}"));
            }

            // Opens on the last run's length, else the first.
            var lengthIndex = System.Array.IndexOf(options.runLengths, RunSettings.Length);
            if (lengthIndex >= 0) levelsDropdown.SetValueWithoutNotify(lengthIndex);

            levelsDropdown.RefreshShownValue();

            // One entry per layout style.
            layoutDropdown.ClearOptions();
            foreach (var (label, _) in Layouts)
            {
                layoutDropdown.options.Add(new TMP_Dropdown.OptionData(label));
            }

            // Opens on the last run's layout, else the first.
            var layoutIndex = System.Array.FindIndex(Layouts, l => l.style == RunSettings.Layout);
            if (layoutIndex >= 0) layoutDropdown.SetValueWithoutNotify(layoutIndex);

            layoutDropdown.RefreshShownValue();
            startRunButton.onClick.AddListener(Play);
        }

        private void Play()
        {
            // Each dropdown was filled from its source in order, so its value indexes straight back into it.
            var profile = options.profiles[difficultyDropdown.value];
            var length = options.runLengths[levelsDropdown.value];
            var layout = Layouts[layoutDropdown.value].style;

            // Remembered so the menu opens on this selection next time.
            RunSettings.Difficulty = profile.label;
            RunSettings.Length = length;
            RunSettings.Layout = layout;
            RunSettings.Save();

            // Stash the selection for RunController, then enter the gameplay scene.
            RunContext.Pending = new RunContext(profile, 1, length, layout);
            SceneManager.LoadScene("Gameplay");
        }
    }
}