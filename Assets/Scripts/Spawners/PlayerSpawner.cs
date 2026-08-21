using System.Collections.Generic;
using System.Linq;
using Camera;
using Generation.Tiles;
using Graphs.Rooms;
using Menu;
using Player;
using Run;
using Simulation;
using UnityEngine;

namespace Spawners
{
    public class PlayerSpawner : EntitySpawner
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private InventoryController inventoryController;

        // The item types a loadout can grant, each carrying the prefab the player is handed for it.
        [SerializeField] private ItemDefinition[] startingItems;

        private GameObject _player;

        public override void Spawn(RoomGraph graph, Dictionary<string, RoomRect> rects, WorldContext world)
        {
            var entrance = rects["room_entrance"];
            var playerSpawn = world.Tilemap.GetCellCenterWorld(new Vector3Int(entrance.CenterX, entrance.CenterY, 0));
            _player = Instantiate(playerPrefab, playerSpawn, Quaternion.identity, transform);

            // Hand the player its world before anything ticks.
            var actor = _player.GetComponent<Actor>();
            actor.Init(world);
            world.BindPlayer(actor); // so other sim participants (e.g. guards) can find the player
            if (cameraFollow) cameraFollow.SetTarget(_player.transform);
            if (inventoryController) inventoryController.Bind(_player.GetComponent<PlayerInventory>());

            ApplyLoadout(world);
        }

        // Fills the spawned player from the pending loadout: shop purchases or carried from previous level.
        private void ApplyLoadout(WorldContext world)
        {
            var loadout = RunLoadout.Pending;
            if (loadout == null) return;
            RunLoadout.Pending = null;

            var inventory = world.Player.GetComponent<PlayerInventory>();
            foreach (var type in loadout.Items)
            {
                var held = Instantiate(PrefabFor(type), transform);
                var item = held.GetComponent<IInventoryItem>();
                item.Bind(world);
                inventory.Grant(item);
                held.SetActive(false);
            }

            // Hearts and inventory settings carried over from previous level.
            if (loadout.StartingHearts is { } hearts && world.Player.TryGetComponent(out PlayerHealth health))
            {
                health.SetHearts(hearts);
            }

            if (loadout.Selection is { } selected && inventory.CountOf(selected) > 0)
            {
                inventory.Select(selected);
            }
        }

        private GameObject PrefabFor(ItemType type)
        {
            return (from item in startingItems where item.type == type select item.prefab).FirstOrDefault();
        }
    }
}