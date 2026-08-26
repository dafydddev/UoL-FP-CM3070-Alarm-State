using System.Collections.Generic;
using Entities.Objectives;
using Spawners;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
    // The objective list on the HUD: the level's primary objective, then whatever secondaries.
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class ObjectiveHud : MonoBehaviour
    {
        // Cloned once per objective, so the font and sizing stay authored in the scene. Left inactive there.
        [SerializeField] private TMP_Text rowTemplate;

        // The row standing for each objective in the current level.
        private readonly Dictionary<Objective, TMP_Text> _rows = new();

        private void OnEnable()
        {
            ObjectiveSpawner.ObjectivesSpawned += Rebuild;
            Objective.Complete += Complete;
        }

        private void OnDisable()
        {
            ObjectiveSpawner.ObjectivesSpawned -= Rebuild;
            Objective.Complete -= Complete;
        }

        // A fresh level replaces the whole list, one pending row per objective it placed.
        private void Rebuild(IReadOnlyList<Objective> objectives)
        {
            foreach (var row in _rows.Values)
            {
                Discard(row.gameObject);
            }

            _rows.Clear();
            if (!rowTemplate) return;
            foreach (var objective in objectives)
            {
                _rows[objective] = CreateRow(objective);
            }
        }

        // Strikes the completed objective's row through.
        private void Complete(Objective objective)
        {
            if (!_rows.TryGetValue(objective, out var row)) return;
            row.fontStyle |= FontStyles.Strikethrough;
        }

        private TMP_Text CreateRow(Objective objective)
        {
            var row = Instantiate(rowTemplate, transform, false); // layout order follows the order they were placed
            row.gameObject.SetActive(true);
            row.gameObject.name = $"Objective_{objective.id}";
            row.text = $"{(objective is PrimaryObjective ? "Primary" : "Secondary")}: {objective.text}";
            row.raycastTarget = false;
            return row;
        }

        // Generate Preview rebuilds levels outside play mode, where Destroy won't run.
        private static void Discard(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}