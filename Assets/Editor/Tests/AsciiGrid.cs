using System;
using System.Collections.Generic;
using Generation.Cells;
using Generation.Facility;
using Generation.Tiles;
using Pathfinding;
using Simulation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.Tests
{
    // A facility written as ASCII rows: "#" wall, anything else is a floor.
    public sealed class AsciiGrid : IDisposable
    {
        public AStarPathfinder Pathfinder { get; }
        private OccupancyMap Occupancy { get; } = new();

        private readonly List<Object> _created = new();

        public AsciiGrid(params string[] rows)
        {
            var floor = Tile(true);
            var wall = Tile(false);
            var width = rows[0].Length;
            var height = rows.Length;

            var tiles = new TileDefinition[width, height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                tiles[x, y] = rows[height - 1 - y][x] == '#' ? wall : floor;

            var scheduler = NewGameObject().AddComponent<Scheduler>();
            Pathfinder = new AStarPathfinder(new EntryRules(new FacilityGrid(tiles), Occupancy, scheduler));
        }

        // Puts something on a cell that refuses everyone, like how a spawned door behaves.
        public void Block(Vector2Int cell)
        {
            var host = NewGameObject();
            host.AddComponent<Barrier>();
            Occupancy.Place(cell, host);
        }

        public void Dispose()
        {
            foreach (var created in _created) Object.DestroyImmediate(created);
            _created.Clear();
        }

        private TileDefinition Tile(bool walkable)
        {
            var tile = ScriptableObject.CreateInstance<TileDefinition>();
            _created.Add(tile);
            var serialized = new SerializedObject(tile);
            serialized.FindProperty("walkable").boolValue = walkable;
            serialized.ApplyModifiedProperties();
            return tile;
        }

        private GameObject NewGameObject()
        {
            var host = new GameObject("ascii grid");
            _created.Add(host);
            return host;
        }

        private class Barrier : MonoBehaviour, IEntryBlocker
        {
            public bool BlocksEntry(Actor mover) => true;
        }
    }
}
