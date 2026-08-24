using System;
using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A level exit. Fires Reached when the player uses it.
    public class Exit : MonoBehaviour, IUseHandler
    {
        public static event Action Reached;

        // Worn while the exit is sealed. The prefab's own sprite is the open look.
        [SerializeField] private Sprite lockedSprite;

        private SpriteRenderer _sprite;
        private Sprite _openSprite;
        private Vector2Int _cell;
        private WorldContext _world;

        // The exit stays locked until the primary objective is completed and seals while the alarm sounds.
        private bool Locked => _world == null || !_world.Mission.PrimaryComplete || _world.Alarm.Active;

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            if (_sprite) _openSprite = _sprite.sprite;
        }

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            _cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(_cell, gameObject);
            world.Mission.PrimaryCompleted += Refresh;
            Refresh();
        }

        // The alarm sealing and clearing the exit rides the static event, as the switches' own tint does.
        private void OnEnable()
        {
            AlarmState.ActiveChanged += OnAlarmChanged;
            Refresh();
        }

        private void OnDisable() => AlarmState.ActiveChanged -= OnAlarmChanged;

        private void OnAlarmChanged(bool active) => Refresh();

        // Puts up whichever sprite matches the state the exit is in.
        private void Refresh()
        {
            if (_sprite && lockedSprite) _sprite.sprite = Locked ? lockedSprite : _openSprite;
        }

        public bool CanUse(Actor user) => user is PlayerActor && !Locked;

        public bool OnUsed(Actor user)
        {
            if (!CanUse(user)) return false;
            Reached?.Invoke();
            return true;
        }

        private void OnDestroy()
        {
            if (_world == null) return;
            _world.Mission.PrimaryCompleted -= Refresh;
            if (_world.Occupancy.At(_cell) == gameObject) _world.Occupancy.Remove(_cell);
        }
    }
}