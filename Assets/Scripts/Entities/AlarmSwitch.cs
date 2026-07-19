using Generation.Cells;
using Player;
using Simulation;
using UnityEngine;

namespace Entities
{
    // A wall-mounted alarm switch. A guard trips it to raise the alarm, the player presses Use on it to disable.
    // Sits on the grid so the use key and guards can find it.
    public class AlarmSwitch : MonoBehaviour, IUseHandler, IAlarmSwitch
    {
        [SerializeField] private Color activeColour = Color.red;

        public Vector2Int Cell { get; private set; }

        private WorldContext _world;
        private SpriteRenderer _renderer;
        private Color _idleColour = Color.white;

        // Called by the spawner after Instantiate.
        public void Init(WorldContext world)
        {
            _world = world;
            Cell = (Vector2Int)world.Tilemap.WorldToCell(transform.position);
            world.Occupancy.Place(Cell, gameObject);
            world.Alarm.Register(this);

            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer) _idleColour = _renderer.color;
        }

        // All switches share the alarm's on/off look, so any switch reads as live while it sounds.
        private void OnEnable() => AlarmState.ActiveChanged += Tint;
        private void OnDisable() => AlarmState.ActiveChanged -= Tint;

        // Tripped by a guard's raise action with the contact it captured.
        public void Activate(Vector2Int contactCell, Vector2Int contactHeading) =>
            _world.Alarm.Raise(contactCell, contactHeading);

        // The player disables the alarm by using any switch while it sounds. Guards do the raising.
        public void OnUsed(Actor user)
        {
            if (user is not PlayerActor || !_world.Alarm.Active) return;
            _world.Alarm.Disable();
        }

        private void Tint(bool active)
        {
            if (_renderer) _renderer.color = active ? activeColour : _idleColour;
        }

        private void OnDestroy()
        {
            if (_world != null && _world.Occupancy.At(Cell) == gameObject) _world.Occupancy.Remove(Cell);
        }
    }
}
