using System;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using UnityEngine;

namespace Navigation
{
    // A* over the world's entry rules:
    // A cell is walkable exactly when the mover could step into it (terrain walkable, no blocking occupant),
    // so paths automatically respect locked doors, keys held, and anything else the rules learn later.
    // Movement is 4-connected (no diagonals).
    public class AStarPathfinder
    {
        private readonly EntryRules _entry;

        public AStarPathfinder(EntryRules entry) => _entry = entry;

        // Walkability is per-mover: a locked door blocks a player when they do not have the right key.
        // Out-of-bounds cells have no tile, so the rules refuse them too.
        public bool IsWalkable(Vector2Int cell, Actor mover) => _entry.CanEnter(cell, mover);

        // Finds a cell path from start to goal for the given mover,
        // or null if either end is blocked or no route exists.
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Actor mover)
        {
            if (!IsWalkable(start, mover) || !IsWalkable(goal, mover)) return null;

            var open = new MinHeap(); // frontier, ordered by f-score
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>(); // best predecessor of each cell
            var gScore = new Dictionary<Vector2Int, int> { [start] = 0 }; // cheapest known cost to reach each cell

            open.Push(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var current = open.Pop();
                if (current == goal) return Reconstruct(cameFrom, current);

                foreach (var nb in Neighbours(current, mover))
                {
                    // Every step costs 1; skip neighbours we already have a cheaper route to.
                    var tentative = gScore[current] + 1;
                    if (gScore.TryGetValue(nb, out var existing) && tentative >= existing) continue;

                    // Record this cheaper route and queue the neighbour by f = g + heuristic.
                    cameFrom[nb] = current;
                    gScore[nb] = tentative;
                    open.Push(nb, tentative + Heuristic(nb, goal));
                }
            }

            return null; // frontier exhausted without reaching the goal
        }

        // The four cardinal step directions (right, left, up, down).
        private static readonly Vector2Int[] Dirs =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        // Yields the cardinal neighbours the mover could step into.
        private IEnumerable<Vector2Int> Neighbours(Vector2Int c, Actor mover)
        {
            return Dirs.Select(d => c + d).Where(n => IsWalkable(n, mover));
        }

        // Manhattan distance — admissible for 4-connected movement.
        private static int Heuristic(Vector2Int a, Vector2Int b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        // Walks the cameFrom chain back from the goal and reverses it into a start-to-goal path.
        private static List<Vector2Int> Reconstruct(
            Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out current)) path.Add(current);
            path.Reverse();
            return path;
        }

        // Binary min-heap keyed by f-score.
        private class MinHeap
        {
            private readonly List<(Vector2Int cell, int priority)> _items = new();
            public int Count => _items.Count;

            // Adds a cell, then sifts it up until the heap order is restored.
            public void Push(Vector2Int cell, int priority)
            {
                _items.Add((cell, priority));
                var i = _items.Count - 1;
                while (i > 0)
                {
                    var parent = (i - 1) / 2;
                    if (_items[parent].priority <= _items[i].priority) break;
                    (_items[parent], _items[i]) = (_items[i], _items[parent]);
                    i = parent;
                }
            }

            // Removes and returns the lowest-priority cell, then sifts the moved last item down.
            public Vector2Int Pop()
            {
                var root = _items[0].cell;
                var last = _items.Count - 1;
                _items[0] = _items[last]; // move last item to the root
                _items.RemoveAt(last);

                var i = 0;
                while (true)
                {
                    // Swap with the smaller of the two children until neither is smaller.
                    int l = 2 * i + 1, r = 2 * i + 2, smallest = i;
                    if (l < _items.Count && _items[l].priority < _items[smallest].priority) smallest = l;
                    if (r < _items.Count && _items[r].priority < _items[smallest].priority) smallest = r;
                    if (smallest == i) break;
                    (_items[smallest], _items[i]) = (_items[i], _items[smallest]);
                    i = smallest;
                }

                return root;
            }
        }
    }
}