using Player;
using UnityEngine;

namespace Audio
{
    // The sounds the player's own actions make.
    // Unlike the scene controllers, this rides the player prefab, so is added and removed to the scene with the player.
    [RequireComponent(typeof(AudioSource))]
    public class PlayerSfx : MonoBehaviour
    {
        [SerializeField] private GameplaySfx footstep;
        [SerializeField] private GameplaySfx collect;
        [SerializeField] private GameplaySfx use;
        [SerializeField] private GameplaySfx hurt;
        [SerializeField] private GameplaySfx enterCover;

        private AudioSource _source;
        private PlayerActor _actor;

        private Vector2Int _cell;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _actor = GetComponent<PlayerActor>();
        }

        // The spawn cell only reads true once the spawner has handed the actor its world.
        // Seeded here so the first Update does not hear the spawn as a step.
        private void Start()
        {
            if (_actor) _cell = _actor.Cell;
        }

        private void OnEnable()
        {
            PlayerInventory.Collected += OnCollected;
            PlayerInventory.Used += OnUsed;
            PlayerKeyring.OnKeycardCollected += OnKeycardCollected;
            PlayerHealth.Damaged += OnDamaged;
            PlayerHiding.OnHiddenChanged += OnHiddenChanged;
        }

        private void OnDisable()
        {
            PlayerInventory.Collected -= OnCollected;
            PlayerInventory.Used -= OnUsed;
            PlayerKeyring.OnKeycardCollected -= OnKeycardCollected;
            PlayerHealth.Damaged -= OnDamaged;
            PlayerHiding.OnHiddenChanged -= OnHiddenChanged;
        }

        // A footstep for each cell the player moves onto.
        private void Update()
        {
            if (!_actor || _actor.Cell == _cell) return;
            _cell = _actor.Cell;
            Play(footstep);
        }

        private void OnCollected(ItemType type) => Play(collect);

        private void OnKeycardCollected(string keyId) => Play(collect);

        private void OnUsed(ItemType type) => Play(use);

        private void OnDamaged() => Play(hurt);

        private void OnHiddenChanged(bool hidden)
        {
            if (hidden) Play(enterCover);
        }

        private void Play(GameplaySfx gameplaySfx)
        {
            if (gameplaySfx) gameplaySfx.Play(_source);
        }
    }
}