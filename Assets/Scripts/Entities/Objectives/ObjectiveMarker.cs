using UnityEngine;

namespace Entities.Objectives
{
    // Drops an objective's outstanding look once its minigame is won:
    // the minimap blip goes off, and the sprite in the level changes.
    [RequireComponent(typeof(Objective))]
    public class ObjectiveMarker : MonoBehaviour
    {
        // The minimap blip child, live only while the objective is still outstanding.
        [SerializeField] private GameObject blip;

        // Put up in the level once the objective is done. Leave unset to hide the objective's sprite instead.
        [SerializeField] private Sprite completedSprite;

        private Objective _objective;
        private SpriteRenderer _sprite;

        private void Awake()
        {
            _objective = GetComponent<Objective>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable() => Objective.Complete += OnComplete;

        private void OnDisable() => Objective.Complete -= OnComplete;

        // Complete covers every objective in the level, so ignore the other rooms' ones.
        private void OnComplete(Objective objective)
        {
            if (objective != _objective) return;

            if (blip) blip.SetActive(false);
            if (!_sprite) return;
            if (completedSprite) _sprite.sprite = completedSprite;
            else _sprite.enabled = false;
        }
    }
}