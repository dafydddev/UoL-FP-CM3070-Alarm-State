using System;
using System.Collections.Generic;
using System.Linq;
using Generation.Missions;

namespace Generation.Rooms
{
    public static class RoomGraphGenerator
    {
        // Expands an abstract mission into a concrete room graph: one room per mission node,
        // Plus an entrance, optional locked keycard rooms, guard posts, connecting corridors, and exits.

        // Builds the room graph for a mission at the given profile and level.
        public static RoomGraph Generate(MissionGraph mission, DifficultyProfile profile, int level)
        {
            var graph = new RoomGraph { seed = mission.seed, level = level };
            var rng = new Random(mission.seed);
            var missionRoomMap = new Dictionary<string, RoomNode>(); // mission node id → its room

            // 1. Create one room per mission node.
            foreach (var node in mission.nodes)
            {
                var room = new RoomNode
                {
                    id = $"room_{node.id}",
                    type = MissionRoleToRoomRole(node.nodeType, node.text),
                    missionNodeId = node.id
                };
                graph.rooms.Add(room);
                missionRoomMap[node.id] = room;
            }

            // 2. Add the entrance and wire it to the mission's entry room.
            var entrance = new RoomNode { id = "room_entrance", type = RoomType.Entrance };
            graph.rooms.Add(entrance);
            graph.edges.Add(new RoomEdge { fromId = entrance.id, toId = missionRoomMap["entry"].id });

            // 3. Turn mission dependencies into edges, locking some objective/keycard rooms.
            var lockChance = profile.lockChance.Evaluate(level);
            var usedKeys = new HashSet<string>();
            foreach (var node in mission.nodes)
            {
                if (!missionRoomMap.TryGetValue(node.id, out var toRoom)) continue;
                foreach (var depId in node.dependencies)
                {
                    if (!missionRoomMap.TryGetValue(depId, out var fromRoom)) continue;

                    // Only objective and keycard rooms are eligible to be locked.
                    var eligible = toRoom.type is RoomType.ObjectiveRoom or RoomType.KeycardRoom;

                    string keyRoomId = null;
                    if (eligible && rng.NextDouble() < lockChance)
                    {
                        // Reuse an existing keycard room earlier in the chain if one is free.
                        var found = FindKeycardRoomInChain(depId, mission, missionRoomMap);
                        if (found != null && usedKeys.Add(found))
                        {
                            keyRoomId = found;
                        }
                        else
                        {
                            // Or spawn a fresh keycard room off the source room.
                            var key = new RoomNode
                                { id = $"room_key_{graph.rooms.Count}", type = RoomType.KeycardRoom };
                            graph.rooms.Add(key);
                            graph.edges.Add(new RoomEdge { fromId = fromRoom.id, toId = key.id });
                            usedKeys.Add(key.id);
                            keyRoomId = key.id;
                        }
                    }

                    // Connect the dependency to its dependent, locked if a key was assigned.
                    graph.edges.Add(new RoomEdge
                    {
                        fromId = fromRoom.id,
                        toId = toRoom.id,
                        locked = keyRoomId != null,
                        keyRoomId = keyRoomId
                    });
                }
            }

            // 4. Insert guard posts in front of objective rooms (always) and keycard rooms (50%).
            var guardCandidates =
                graph.rooms.FindAll(r => r.type is RoomType.ObjectiveRoom or RoomType.KeycardRoom);
            foreach (var candidate in guardCandidates)
            {
                if (!(candidate.type == RoomType.ObjectiveRoom || rng.NextDouble() > 0.5)) continue;

                var guard = new RoomNode { id = $"room_guard_{graph.rooms.Count}", type = RoomType.GuardPost };
                graph.rooms.Add(guard);

                // Splice the guard post onto the candidate's inbound edge, moving any lock to the new edge.
                var inbound = graph.edges.Find(e => e.toId == candidate.id);
                if (inbound != null)
                {
                    graph.edges.Add(new RoomEdge
                    {
                        fromId = guard.id, toId = candidate.id, locked = inbound.locked,
                        keyRoomId = inbound.keyRoomId
                    });
                    inbound.toId = guard.id; // redirect the old edge into the guard post
                    inbound.locked = false;
                    inbound.keyRoomId = null;
                }
                else
                {
                    graph.edges.Add(new RoomEdge { fromId = guard.id, toId = candidate.id });
                }
            }

            // 5. Insert a corridor between most connected rooms (skip ones already corridor/entrance/exit).
            var snapshot = new List<RoomEdge>(graph.edges); // iterate a copy since we mutate the list
            foreach (var edge in snapshot)
            {
                var from = graph.GetRoom(edge.fromId);
                var to = graph.GetRoom(edge.toId);
                if (from == null || to == null) continue;
                if (from.type is RoomType.Corridor or RoomType.Entrance) continue;
                if (to.type is RoomType.Corridor or RoomType.Exit) continue;

                // Replace the direct edge with from -> corridor -> to, carrying the lock onto the second half.
                var corridor = new RoomNode { id = $"room_corridor_{graph.rooms.Count}", type = RoomType.Corridor };
                graph.rooms.Add(corridor);
                var idx = graph.edges.IndexOf(edge);
                if (idx != -1) graph.edges.RemoveAt(idx);
                graph.edges.Add(new RoomEdge { fromId = edge.fromId, toId = corridor.id });
                graph.edges.Add(new RoomEdge
                    { fromId = corridor.id, toId = edge.toId, locked = edge.locked, keyRoomId = edge.keyRoomId });
            }

            // 6. Add exits. There's always one from the primary objective room.
            var exitCount = (int)profile.exitCount.Evaluate(level);

            var primaryRoom = graph.rooms.Find(r => r.type == RoomType.ObjectiveRoom && r.missionNodeId == "primary");
            var primaryExit = new RoomNode { id = "room_exit_0", type = RoomType.Exit };
            graph.rooms.Add(primaryExit);

            if (exitCount <= 0)
            {
                // No free exits at this level: the only way out is back through the entrance.
                graph.edges.Add(new RoomEdge { fromId = entrance.id, toId = primaryExit.id });
            }
            else if (primaryRoom != null)
            {
                graph.edges.Add(new RoomEdge { fromId = primaryRoom.id, toId = primaryExit.id });
            }

            // Add extra exits on non-critical rooms if the level allows more than one.
            if (exitCount > 1)
            {
                // Rooms on the critical path must not host an alternate exit.
                var criticalIds = new HashSet<string>(
                    mission.nodes
                        .Where(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite or NodeType.Primary)
                        .Select(n => n.id)
                );

                // Eligible hosts: corridors / objective rooms that aren't critical or the primary.
                var exitCandidates = graph.rooms
                    .Where(r => r.type is RoomType.Corridor or RoomType.ObjectiveRoom)
                    .Where(r => r.missionNodeId == null || !criticalIds.Contains(r.missionNodeId))
                    .Where(r => r.id != primaryExit.id && r.id != primaryRoom?.id)
                    .ToList();

                // Fisher–Yates shuffle so the chosen exit rooms vary by seed.
                for (var i = exitCandidates.Count - 1; i > 0; i--)
                {
                    var j = rng.Next(i + 1);
                    (exitCandidates[i], exitCandidates[j]) = (exitCandidates[j], exitCandidates[i]);
                }

                // Attach as many additional exits as we have room for.
                var additionalCount = Math.Min(exitCount - 1, exitCandidates.Count);
                for (var i = 0; i < additionalCount; i++)
                {
                    var exit = new RoomNode { id = $"room_exit_{i + 1}", type = RoomType.Exit };
                    graph.rooms.Add(exit);
                    graph.edges.Add(new RoomEdge { fromId = exitCandidates[i].id, toId = exit.id });
                }
            }

            return graph;
        }

        // Maps a mission node's type (and text) to the room role it should become.
        // Prerequisites become keycard rooms if their text mentions card/code/badge, else guard posts.
        private static RoomType MissionRoleToRoomRole(NodeType nodeType, string text)
        {
            return nodeType switch
            {
                NodeType.Entry => RoomType.Entrance,
                NodeType.Primary => RoomType.ObjectiveRoom,
                NodeType.Secondary => RoomType.ObjectiveRoom,
                NodeType.Prerequisite => text.ToLower().Contains("card") || text.ToLower().Contains("code") ||
                                         text.ToLower().Contains("badge")
                    ? RoomType.KeycardRoom
                    : RoomType.GuardPost,
                _ => RoomType.Corridor
            };
        }

        // Searches the dependency chain upstream of a mission node for an existing keycard room,
        // returning its room id, or null if none is found. (Depth-first over dependencies.)
        private static string FindKeycardRoomInChain(string fromMissionId, MissionGraph mission,
            Dictionary<string, RoomNode> map)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(fromMissionId);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!visited.Add(id)) continue; // skip already-seen nodes
                if (map.TryGetValue(id, out var room) && room.type == RoomType.KeycardRoom) return room.id;
                var node = mission.nodes.Find(n => n.id == id);
                if (node == null) continue;
                foreach (var dep in node.dependencies) stack.Push(dep);
            }

            return null;
        }
    }
}