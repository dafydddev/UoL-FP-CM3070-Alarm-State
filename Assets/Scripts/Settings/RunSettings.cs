using Generation.Tiles;
using UnityEngine;

namespace Settings
{
    // The play menu's last selection, so it opens on it next time.
    // Stored by value rather than dropdown index, so reordering the run options doesn't remap it.
    public static class RunSettings
    {
        private const string DifficultyKey = "RunDifficulty";
        private const string LengthKey = "RunLength";
        private const string LayoutKey = "RunLayout";

        // A difficulty profile's label; empty until a run has been started.
        public static string Difficulty
        {
            get => PlayerPrefs.GetString(DifficultyKey, "");
            set => PlayerPrefs.SetString(DifficultyKey, value);
        }

        // A level count; 0 until a run has been started.
        public static int Length
        {
            get => PlayerPrefs.GetInt(LengthKey, 0);
            set => PlayerPrefs.SetInt(LengthKey, value);
        }

        public static TileLayoutStyle Layout
        {
            get => (TileLayoutStyle)PlayerPrefs.GetInt(LayoutKey, (int)TileLayoutStyle.Spine);
            set => PlayerPrefs.SetInt(LayoutKey, (int)value);
        }

        // The setters only write to the in-memory prefs, so a change is lost unless this is called.
        public static void Save() => PlayerPrefs.Save();
    }
}
