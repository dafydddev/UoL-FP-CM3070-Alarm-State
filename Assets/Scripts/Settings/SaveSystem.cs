using System;
using System.Collections.Generic;
using System.IO;
using Player;
using Tutorials;
using UnityEngine;

namespace Settings
{
    // The persisted profile, as JsonUtility writes it to the file.
    [Serializable]
    public sealed class SaveData
    {
        public int currencyBalance;

        // Serialized by enum value, so ItemType is append-only: reordering would remap owned items.
        public List<ItemType> ownedItems = new();

        // The item types whose upgrade has been bought. One entry each: an upgrade is bought once and kept.
        public List<ItemType> upgradedItems = new();

        // The skins bought. One entry each: a skin is bought once and kept.
        public List<SkinType> boughtSkins = new();

        // The skin the player appears in.
        public SkinType equippedSkin;

        // Serialized by enum value, so TutorialTopic is append-only too.
        public List<TutorialTopic> seenTutorials = new();
    }

    // Reads and writes the profile as JSON under persistentDataPath.
    public static class SaveSystem
    {
        private static readonly string Path =
            System.IO.Path.Combine(Application.persistentDataPath, "profile.json");

        // Loaded once on first access.
        public static SaveData Data { get; private set; } = Load();

        private static SaveData Load()
        {
            try
            {
                if (!File.Exists(Path)) return new SaveData();
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(Path)) ?? new SaveData();
            }
            catch
            {
                // Corrupt or unreadable: start fresh rather than throw from a static initializer.
                return new SaveData();
            }
        }

        // Write to a temp file before locking it in, so a failed attempt doesn't corrupt the live one.
        public static void Save()
        {
            var json = JsonUtility.ToJson(Data);
            var temp = Path + ".tmp";
            try
            {
                File.WriteAllText(temp, json);
                if (File.Exists(Path)) File.Replace(temp, Path, null);
                else File.Move(temp, Path);
            }
            catch
            {
                File.WriteAllText(Path, json);
            }
        }

#if UNITY_EDITOR
        // Debug: back to a fresh profile.
        [UnityEditor.MenuItem("Tools/Reset Save")]
        public static void Reset()
        {
            Data = new SaveData();
            Save();
        }
        [UnityEditor.MenuItem("Tools/Gift Currency")]
        public static void GiftCurrency() => Data.currencyBalance += 10000;
#endif
    }
}
