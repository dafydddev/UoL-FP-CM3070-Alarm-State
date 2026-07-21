using System.Collections.Generic;
using Entities.Keycards;
using Player;
using Spawners;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    // The row of keycard slots on the HUD: one per key in the level, faded until the player picks it up.
    // Each slot carries its key's colour, the same hue KeyColour gives the card and the door it opens.
    // Slots are built per level as the number of keys varies with how much the level locked (see RunDifficulty.lockChance).
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class KeycardHud : MonoBehaviour
    {
        [SerializeField] private Sprite keycardIcon;
        [SerializeField] private Vector2 iconSize = new(32f, 32f);

        // How far an uncollected slot fades, so the row reads as the keys still to find.
        [SerializeField, Range(0f, 1f)] private float missingAlpha = 0.25f;

        // The slot standing for each key id in the current level.
        private readonly Dictionary<string, Image> _slots = new();

        private void OnEnable()
        {
            KeycardSpawner.KeysSpawned += Rebuild;
            PlayerKeyring.OnKeycardCollected += Fill;
        }

        private void OnDisable()
        {
            KeycardSpawner.KeysSpawned -= Rebuild;
            PlayerKeyring.OnKeycardCollected -= Fill;
        }

        // A fresh level replaces the whole row, one faded slot per key it placed.
        private void Rebuild(IReadOnlyList<string> keyIds, int seed)
        {
            foreach (var slot in _slots.Values)
            {
                Discard(slot.gameObject);
            }

            _slots.Clear();
            foreach (var keyId in keyIds)
            {
                _slots[keyId] = CreateSlot(keyId, Fade(KeyColour.For(keyId, seed), missingAlpha));
            }
        }

        // Brings the collected key's slot up to full strength, keeping its hue.
        private void Fill(string keyId)
        {
            if (_slots.TryGetValue(keyId, out var slot)) slot.color = Fade(slot.color, 1f);
        }

        private Image CreateSlot(string keyId, Color colour)
        {
            var go = new GameObject($"Keycard_{keyId}", typeof(Image)) { layer = gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false); // layout order follows the order keys were placed
            rect.sizeDelta = iconSize;
            var image = go.GetComponent<Image>();
            image.sprite = keycardIcon;
            image.color = colour;
            image.raycastTarget = false;
            return image;
        }

        private static Color Fade(Color colour, float alpha) => new(colour.r, colour.g, colour.b, alpha);

        // Generate Preview rebuilds levels outside play mode, where Destroy won't run.
        private static void Discard(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}