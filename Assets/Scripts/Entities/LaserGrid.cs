using System.Collections.Generic;
using Effects;
using Generation.Lasers;
using Generation.Tiles;
using Simulation;
using UnityEngine;

namespace Entities
{
    // The lasers in a pressure room. Cycles on the scheduler's tick and raises the alarm
    // when the player is in a live beam. Guards are ignored.
    public class LaserGrid : Actor
    {
        [SerializeField] private Color beamColour = Color.red;
        [SerializeField] private int sortingOrder = 1;

        private readonly List<Beam> _beams = new();
        private readonly List<Vector3> _litCentres = new();

        private RoomRect _rect;
        private int _cyclePeriod = 16;

        private CellSurface _surface;
        private Vector2 _half = Vector2.one * 0.5f;

        private int _tick;
        private Vector2Int _anchor;
        private Vector2Int _lastPlayerCell;
        private bool _seenPlayer;

        // A wall cell, so IsClear never counts the grid as standing on a floor cell.
        public override Vector2Int Cell => _anchor;

        private sealed class Beam
        {
            public LaserSpec Spec;
            public List<Vector2Int> Cells;
            public bool Live;
        }

        // Called by the spawner after Instantiate, with the layout it drew for this room.
        public void Init(WorldContext world, RoomRect rect, IReadOnlyList<LaserSpec> lasers, int cyclePeriod)
        {
            _rect = rect;
            _cyclePeriod = Mathf.Max(2, cyclePeriod);
            _anchor = new Vector2Int(rect.X, rect.Y);

            base.Init(world); // before the beams, which read the grid through World

            _surface = new CellSurface(transform, "Lasers", sortingOrder);
            _half = (Vector2)world.Tilemap.cellSize * 0.5f;

            foreach (var spec in lasers)
                _beams.Add(new Beam
                {
                    Spec = spec,
                    Cells = LaserGridLayout.BeamCells(spec, rect, Blocked),
                    Live = LaserGridLayout.IsLive(spec, _tick, _cyclePeriod),
                });

            Repaint();
        }

        protected override void Act()
        {
            _tick++;
            Refresh();

            var player = World.Player;
            if (!player)
            {
                _seenPlayer = false;
                return;
            }

            var cell = player.Cell;
            var heading = _seenPlayer ? Heading(_lastPlayerCell, cell) : Vector2Int.zero;
            _lastPlayerCell = cell;
            _seenPlayer = true;

            // Raise no-ops while the alarm sounds, so standing in a beam doesn't raise it again.
            if (IsLive(cell)) World.Alarm.Raise(cell, heading);
        }

        private void Refresh()
        {
            var changed = false;
            foreach (var beam in _beams)
            {
                var live = LaserGridLayout.IsLive(beam.Spec, _tick, _cyclePeriod);
                if (live == beam.Live) continue;
                beam.Live = live;
                changed = true;
            }

            if (changed) Repaint();
        }

        // Paints the cells that are firing. A laser that is off paints nothing.
        private void Repaint()
        {
            _litCentres.Clear();
            foreach (var beam in _beams)
            {
                if (!beam.Live) continue;
                foreach (var cell in beam.Cells)
                    _litCentres.Add(World.Tilemap.GetCellCenterWorld((Vector3Int)cell));
            }

            _surface.Rebuild(_litCentres, _half, beamColour);
        }

        private bool IsLive(Vector2Int cell)
        {
            foreach (var beam in _beams)
                if (beam.Live && beam.Cells.Contains(cell))
                    return true;
            return false;
        }

        private bool Blocked(Vector2Int cell)
        {
            var tile = World.Grid.At(cell);
            return !tile || tile.BlocksEntry(null);
        }

        // The dominant cardinal from one cell to another, zero if they coincide.
        private static Vector2Int Heading(Vector2Int from, Vector2Int to)
        {
            var d = to - from;
            if (d == Vector2Int.zero) return Vector2Int.zero;
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) return new Vector2Int(d.x > 0 ? 1 : -1, 0);
            return new Vector2Int(0, d.y > 0 ? 1 : -1);
        }

        private void OnDestroy() => _surface?.Dispose();
    }
}