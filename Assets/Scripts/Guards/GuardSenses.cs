using System;
using Entities;
using Player;
using Simulation;
using UnityEngine;

namespace Guards
{
    // The guard's eyes and ears, tuned per guard in the inspector.
    // Vision works entirely on the grid, rather than physics.
    // Guards have a sense range, a facing cone (with a point-blank bubble), and Bresenham line of sight.
    // Sense() runs once per tick and writes what it establishes into GuardMemory.
    [Serializable]
    public class GuardSenses
    {
        [SerializeField, Min(1)] private int viewRangeCells = 7;
        [SerializeField, Range(10f, 360f)] private float viewAngleDegrees = 140f;
        [SerializeField, Min(0)] private int pointBlankCells = 1;
        [SerializeField, Min(0)] private int hearingRangeCells = 9;

        // On losing sight, follow the player's momentum:
        // investigate a point projected this many cells ahead of where they were last seen.
        [SerializeField] private bool projectLostLeadForward = true;
        [SerializeField, Min(1)] private int leadProjectionCells = 3;

        public int HearingRangeCells => hearingRangeCells;
        public int ViewRangeCells => viewRangeCells;

        // Where this guard watched the player hide; null once it sees them out of it.
        private Vector2Int? _hidInView;

        public void Sense(WorldContext world, GridMotor motor, GuardMemory memory)
        {
            var player = world.Player;
            if (!player)
            {
                if (memory.SeesPlayer) memory.NotePlayerLost(memory.PlayerCell);
                return;
            }

            var playerCell = (Vector2Int)world.Tilemap.WorldToCell(player.transform.position);
            if (CanSeePlayer(world, motor, memory, player, playerCell)) memory.NotePlayerSeen(playerCell);
            else if (memory.SeesPlayer) memory.NotePlayerLost(LostLeadCell(world, memory));
        }

        // Where to send a guard that has just lost the player: normally the last-seen cell,
        // but with projection on, a point a few cells further along the player's heading — so the guard heads
        // where they were likely running to instead of pulling up short at the spot they vanished from.
        private Vector2Int LostLeadCell(WorldContext world, GuardMemory memory)
        {
            if (!projectLostLeadForward || memory.PlayerHeading == Vector2Int.zero) return memory.PlayerCell;

            // Walk forward along the heading, stopping at the last open cell before any wall.
            var lead = memory.PlayerCell;
            var probe = memory.PlayerCell;
            for (var i = 0; i < leadProjectionCells; i++)
            {
                probe += memory.PlayerHeading;
                var tile = world.Grid.At(probe);
                if (!tile || tile.BlocksEntry(null)) break;
                lead = probe;
            }

            return lead;
        }

        private bool CanSeePlayer(WorldContext world, GridMotor motor, GuardMemory memory, Actor player, Vector2Int playerCell)
        {
            if (!CanSee(world, motor, playerCell)) return false;

            // Cover hides the player from being spotted, but doesn't break a gaze already fixed on them.
            var hidden = player.TryGetComponent(out PlayerHiding hiding) && hiding.IsHidden;

            // A worn disguise does the same, and just as much stops working once a guard is watching.
            var disguised = player.TryGetComponent(out PlayerDisguise disguise) && disguise.IsDisguised;

            // Taking it in plain view blows it for good: breaking the gaze on the way over
            // doesn't win the trick back, only stepping off that cell unwatched does.
            if (memory.SeesPlayer) _hidInView = hidden || disguised ? playerCell : null;

            return !(hidden || disguised) || memory.SeesPlayer || playerCell == _hidInView;
        }

        // The guard's field of view.
        // A cell is visible when it's in range, inside the facing cone, and not screened by terrain.
        public bool CanSee(WorldContext world, GridMotor motor, Vector2Int cell)
        {
            var offset = cell - motor.Cell;
            var distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            if (distance > viewRangeCells) return false;

            // Outside the view cone counts as unseen, except at point-blank range.
            if (distance > pointBlankCells &&
                Vector2.Angle(motor.Facing, offset) > viewAngleDegrees * 0.5f) return false;

            return HasLineOfSight(world, motor.Cell, cell);
        }

        // Walks the Bresenham line between the two cells.
        // Sight is blocked by any unwalkable terrain in between and closed locked doors.
        private static bool HasLineOfSight(WorldContext world, Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(to.x - from.x), dy = -Mathf.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1, sy = from.y < to.y ? 1 : -1;
            var err = dx + dy;
            var cell = from;

            while (cell != to)
            {
                var e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    cell.x += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    cell.y += sy;
                }

                if (cell == to) break; // the target cell itself never blocks the view of it

                var tile = world.Grid.At(cell);
                if (!tile || tile.BlocksEntry(null)) return false;

                var occupant = world.Occupancy.At(cell);
                if (occupant && occupant.TryGetComponent(out LockedDoor door) && door.BlocksSight) return false;
            }

            return true;
        }
    }
}