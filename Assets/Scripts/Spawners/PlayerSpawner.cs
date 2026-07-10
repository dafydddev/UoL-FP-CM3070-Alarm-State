using System.Collections.Generic;
using Camera;
using Generation.Facility;
using Generation.Tiles;
using Graphs.Rooms;
using Simulation;
using UnityEngine;

namespace Spawners
{
    public class PlayerSpawner : EntitySpawner
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;

        private GameObject _player;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            var entrance = rects["room_entrance"];
            var playerSpawn = world.Tilemap.GetCellCenterWorld(new Vector3Int(entrance.CenterX, entrance.CenterY, 0));
            _player = Instantiate(playerPrefab, playerSpawn, Quaternion.identity, transform);

            // Hand the player its world before anything ticks.
            var actor = _player.GetComponent<Actor>();
            actor.Init(world);
            if (cameraFollow) cameraFollow.SetTarget(_player.transform);
        }
    }
}