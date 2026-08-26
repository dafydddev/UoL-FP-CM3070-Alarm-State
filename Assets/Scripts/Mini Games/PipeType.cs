using System;
using UnityEngine;

namespace Mini_Games
{
    // The four sides of a pipe tile, as a mask so a tile's open ends can be tested together.
    // Bits run clockwise from North, which is what lets Rotated() spin a mask with a shift.
    [Flags]
    public enum PipeDirection
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    // The pipe shapes a board cell can hold, which drives both sprites and connectivity.
    public enum PipeType
    {
        Cap, // one end: a capped dead-end stub
        Straight, // two opposite ends
        Elbow, // two adjacent ends
        Tee, // three ends
        Cross // all four ends
    }

    public static class PipeDirectionExtensions
    {
        // The mask spun by the given number of clockwise quarter turns.
        public static PipeDirection Rotated(this PipeDirection ends, int quarterTurns)
        {
            var bits = (int)ends;
            var turns = quarterTurns & 3;
            return (PipeDirection)(((bits << turns) | (bits >> (4 - turns))) & 0xF);
        }

        // The side a neighbouring tile would have to open to meet this one.
        public static PipeDirection Opposite(this PipeDirection side) => side.Rotated(2);

        // The cell offset of the given side, matching the world grid's y-up convention.
        public static Vector2Int Offset(this PipeDirection side) => side switch
        {
            PipeDirection.North => Vector2Int.up,
            PipeDirection.East => Vector2Int.right,
            PipeDirection.South => Vector2Int.down,
            _ => Vector2Int.left,
        };
    }

    public static class PipeTypeExtensions
    {
        // Each shape's open ends before any rotation is applied, matching how the sprites are drawn.
        // The cap points down, the straight runs top to bottom, the elbow joins bottom to right,
        // the tee opens up, down and right, the cross opens everywhere.
        public static PipeDirection Ends(this PipeType type) => type switch
        {
            PipeType.Cap => PipeDirection.South,
            PipeType.Straight => PipeDirection.North | PipeDirection.South,
            PipeType.Elbow => PipeDirection.East | PipeDirection.South,
            PipeType.Tee => PipeDirection.North | PipeDirection.East | PipeDirection.South,
            _ => PipeDirection.North | PipeDirection.East | PipeDirection.South | PipeDirection.West,
        };
    }

    // One rotatable pipe piece on the game board.
    public class PipeTile
    {
        public Vector2Int Cell;
        public PipeType Type;
        public int Rotation; // clockwise quarter turns, 0-3

        // The sides currently open: the shape's ends spun by the current rotation.
        public PipeDirection Connections => Type.Ends().Rotated(Rotation);

        public void Rotate() => Rotation = (Rotation + 1) & 3;
    }
}