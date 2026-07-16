using System;
using Player;
using Simulation;
using UnityEngine;

namespace Guards
{
    // The guard's eyes and ears, tuned per guard in the inspector.
    // Vision works entirely on the grid: range, a facing cone (with a point-blank bubble),
    // and Bresenham line of sight through the terrain — no physics involved.
    // Sense() runs once per tick and writes what it establishes into GuardMemory.
    [Serializable]
    public class GuardSenses
    {
        [SerializeField, Min(1)] private int viewRangeCells = 7;
        [SerializeField, Range(10f, 360f)] private float viewAngleDegrees = 140f;
        [SerializeField, Min(0)] private int pointBlankCells = 1;
        [SerializeField, Min(0)] private int hearingRangeCells = 9;

        public int HearingRangeCells => hearingRangeCells;

        public void Sense(WorldContext world, GridMotor motor, GuardMemory memory)
        {
            var player = world.Player;
            if (!player)
            {
                memory.NotePlayerLost();
                return;
            }

            var playerCell = (Vector2Int)world.Tilemap.WorldToCell(player.transform.position);
            if (CanSeePlayer(world, motor, memory, player, playerCell)) memory.NotePlayerSeen(playerCell);
            else memory.NotePlayerLost();
        }

        private bool CanSeePlayer(WorldContext world, GridMotor motor, GuardMemory memory,
            Actor player, Vector2Int playerCell)
        {
            var offset = playerCell - motor.Cell;
            var distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            if (distance > viewRangeCells) return false;

            // Outside the view cone counts as unseen, except at point-blank range.
            if (distance > pointBlankCells &&
                Vector2.Angle(motor.Facing, offset) > viewAngleDegrees * 0.5f) return false;

            if (!HasLineOfSight(world, motor.Cell, playerCell)) return false;

            // Cover hides the player from being spotted, but doesn't break a gaze already fixed on them.
            var hidden = player.TryGetComponent(out PlayerHiding hiding) && hiding.IsHidden;
            return !hidden || memory.SeesPlayer;
        }

        // Walks the Bresenham line between the two cells; sight is blocked by any
        // unwalkable terrain in between (doors are occupants, so doorways stay see-through).
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
            }

            return true;
        }
    }
}
