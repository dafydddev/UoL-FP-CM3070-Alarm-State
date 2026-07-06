using Camera;
using Generation.Layout;
using Player;
using Simulation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Generation.Spawners
{
    // Spawns (or respawns) the player prefab, hands it the world it moves through,
    // and registers it with the scheduler so it takes turns.
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;

        private GameObject _player;
        private PlayerActor _actor;
        private Scheduler _scheduler;

        public void SpawnPlayer(Vector3 spawnPosition, FacilityGrid grid, Tilemap tilemap, Scheduler scheduler)
        {
            ClearPlayer();

            _scheduler = scheduler;
            _player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            _actor = _player.GetComponent<PlayerActor>();
            _actor.Bind(grid, tilemap);
            scheduler.Register(_actor);
            if (cameraFollow) cameraFollow.SetTarget(_player.transform);
        }

        public void ClearPlayer()
        {
            if (!_player) return;

            if (_scheduler) _scheduler.Unregister(_actor);
            if (Application.isPlaying) Destroy(_player);
            else DestroyImmediate(_player);

            _player = null;
            _actor = null;
        }
    }
}