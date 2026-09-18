using UnityEngine;

namespace Guards
{
    // Shows the guard's spotted icon for a moment when it first sees the player.
    [RequireComponent(typeof(GuardAgent))]
    public class GuardSpottedIcon : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField, Min(0f)] private float showSeconds = 1f;

        private GuardAgent _agent;
        private bool _sawPlayer;
        private float _hideAt;

        private void Awake()
        {
            _agent = GetComponent<GuardAgent>();
            icon.enabled = false;
        }

        private void LateUpdate()
        {
            var sees = _agent.Memory.SeesPlayer;
            if (sees && !_sawPlayer)
            {
                icon.enabled = true;
                _hideAt = Time.time + showSeconds;
            }
            _sawPlayer = sees;

            if (icon.enabled && Time.time >= _hideAt) icon.enabled = false;
        }
    }
}
