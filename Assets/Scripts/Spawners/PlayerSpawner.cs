using System.Collections.Generic;
using Camera;
using Generation.Facility;
using Graphs.Rooms;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Spawners
{
    public class PlayerSpawner : Spawner
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;
        
        private GameObject _player;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, Tilemap tilemap)
        {
            var entrance = rects["room_entrance"];
            var playerSpawn = tilemap.GetCellCenterWorld(new Vector3Int(entrance.CenterX, entrance.CenterY, 0));
            _player = Instantiate(playerPrefab, playerSpawn, Quaternion.identity, transform);
            if (cameraFollow) cameraFollow.SetTarget(_player.transform);
        }
    }
}