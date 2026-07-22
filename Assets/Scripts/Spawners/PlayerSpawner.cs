using System;
using System.Collections.Generic;
using System.Linq;
using Camera;
using Generation.Tiles;
using Graphs.Rooms;
using HUD;
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
        [SerializeField] private InventoryScreen inventoryScreen;

        // The prefab granted for each item kind bought in the shop.
        [SerializeField] private StartingItem[] startingItems;

        // One item kind's prefab.
        [Serializable]
        private class StartingItem
        {
            public ItemKind kind;
            public GameObject prefab;
        }

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
            if (inventoryScreen) inventoryScreen.Bind(_player.GetComponent<PlayerInventory>());

            ApplyLoadout(world);
        }

        // Seeds the spawned player with the loadout the level starts on: the items bought in the shop,
        // or the health and inventory carried level-to-level. Read once as the level starts, then cleared.
        private void ApplyLoadout(WorldContext world)
        {
            var loadout = RunLoadout.Pending;
            if (loadout == null) return;
            RunLoadout.Pending = null;

            var inventory = world.Player.GetComponent<PlayerInventory>();
            foreach (var kind in loadout.Items)
            {
                var held = Instantiate(PrefabFor(kind), transform);
                var item = held.GetComponent<IInventoryItem>();
                item.Bind(world);
                inventory.Collect(item);
                held.SetActive(false);
            }

            // A carried-over player keeps the hearts level-to-level and the use slot they finished the last level with.
            if (loadout.StartingHearts is { } hearts && world.Player.TryGetComponent(out PlayerHealth health))
            {
                health.SetHearts(hearts);
            }

            if (loadout.Selection is { } selected && inventory.CountOf(selected) > 0)
            {
                inventory.Select(selected);
            }
        }

        private GameObject PrefabFor(ItemKind kind)
        {
            return (from item in startingItems where item.kind == kind select item.prefab).FirstOrDefault();
        }
    }
}