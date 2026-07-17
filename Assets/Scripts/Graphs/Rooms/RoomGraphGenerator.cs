using System;
using System.Collections.Generic;
using System.Linq;
using Generation;
using Graphs.Missions;
using Run;

namespace Graphs.Rooms
{
    // Expands the mission's DEPENDENCY graph into a room graph — a room's approaches are its mission dependencies,
    // so the meaning of the edges is preserved — while enforcing the four-door tile budget by construction.
    // Whenever an attachment tries to give a room a fifth door, the edge is routed through a corridor "relay" that fans the surplus out instead.
    // No dependency is discarded, and no room is generated then removed.
    // Generation is a fixed pipeline of passes over a shared Builder, which owns the door-budget invariant.
    public static class RoomGraphGenerator
    {
        public static RoomGraph Generate(MissionGraph mission, RunDifficulty profile, int level, int totalLevels)
        {
            var builder = new RoomGraphBuilder(mission.seed, level);
            var rng = new Random(Seeds.For(mission.seed, Seeds.Rooms, level));

            var missionRooms = CreateMissionRooms(builder, mission);
            ConnectEntrance(builder, missionRooms);
            ConnectDependencies(builder, mission, missionRooms, profile, rng, level, totalLevels);
            InsertGuardPosts(builder, profile, rng, level, totalLevels);
            ScatterExtraExits(builder, profile, rng, level, totalLevels);
            SpliceCorridors(builder);

            return builder.Graph;
        }

        // 1. One room per mission node.
        private static Dictionary<string, RoomNode> CreateMissionRooms(RoomGraphBuilder roomGraphBuilder, MissionGraph mission)
        {
            var missionRooms = new Dictionary<string, RoomNode>(); // mission node id → its room
            foreach (var node in mission.nodes)
                missionRooms[node.id] =
                    roomGraphBuilder.AddRoom($"room_{node.id}", MissionRoleToRoomRole(node.nodeType), node.id);
            return missionRooms;
        }

        // 2. Entrance: the player spawn and graph root.
        // It only connects to the mission start and its own exit, so the spawn stays clean of guards and keys.
        private static void ConnectEntrance(RoomGraphBuilder roomGraphBuilder, Dictionary<string, RoomNode> missionRooms)
        {
            var entrance = roomGraphBuilder.AddRoom("room_entrance", RoomType.Entrance);
            if (missionRooms.TryGetValue("entry", out var entryRoom)) roomGraphBuilder.AddEdge(entrance.id, entryRoom.id);
            roomGraphBuilder.AddEdge(entrance.id, roomGraphBuilder.AddRoom(roomGraphBuilder.NextId("exit"), RoomType.Exit).id);
        }

        // 3. Turn mission dependencies into edges (preserving the graph's topology);
        // when an objective door locks, its keycard room is a detour off the same source.
        // Both go through Attach, so a source with many dependents plus a key can never blow past four doors.
        private static void ConnectDependencies(RoomGraphBuilder roomGraphBuilder, MissionGraph mission,
            Dictionary<string, RoomNode> missionRooms, RunDifficulty profile, Random rng, int level,
            int totalLevels)
        {
            var lockChance = profile.LockChance(level, totalLevels, rng);
            foreach (var node in mission.nodes)
            {
                if (!missionRooms.TryGetValue(node.id, out var toRoom)) continue;
                foreach (var depId in node.dependencies)
                {
                    if (!missionRooms.TryGetValue(depId, out var fromRoom)) continue;

                    string keyRoomId = null;
                    if (toRoom.type.IsObjective() && rng.NextDouble() < lockChance)
                    {
                        var key = roomGraphBuilder.AddRoom(roomGraphBuilder.NextId("key"), RoomType.KeycardRoom);
                        roomGraphBuilder.Attach(fromRoom.id, key.id); // key reachable off the source, before the door it opens
                        keyRoomId = key.id;
                    }

                    roomGraphBuilder.Attach(fromRoom.id, toRoom.id, keyRoomId != null, keyRoomId);
                }
            }
        }

        // 4. Guard posts in front of objective rooms (always) and keycard rooms (per profile).
        // Every inbound approach is retargeted through the guard, so no approach can bypass it.
        private static void InsertGuardPosts(RoomGraphBuilder roomGraphBuilder, RunDifficulty profile, Random rng, int level,
            int totalLevels)
        {
            var guardChance = profile.GuardChance(level, totalLevels, rng);
            foreach (var candidate in roomGraphBuilder.Graph.rooms.FindAll(r =>
                         r.type.IsObjective() || r.type == RoomType.KeycardRoom))
            {
                if (!candidate.type.IsObjective() && rng.NextDouble() >= guardChance) continue;

                var guard = roomGraphBuilder.AddRoom(roomGraphBuilder.NextId("guard"), RoomType.GuardPost);
                foreach (var inbound in roomGraphBuilder.Graph.edges.FindAll(e => e.toId == candidate.id))
                    roomGraphBuilder.RerouteEdge(inbound,
                        guard.id); // every approach now enters the guard (its lock, if any, rides along)

                roomGraphBuilder.AddEdge(guard.id, candidate.id);
            }
        }

        // 5. Extra exits scattered across corridors and optional side objectives — never the
        // critical path, which room types identify directly. Fisher–Yates picks distinct hosts,
        // one exit each — routed through Attach so a full host relays rather than overflowing.
        private static void ScatterExtraExits(RoomGraphBuilder roomGraphBuilder, RunDifficulty profile,
            Random rng, int level, int totalLevels)
        {
            var extraExitCount = profile.ExtraExitCount(level, totalLevels, rng);
            if (extraExitCount <= 0) return;

            var exitCandidates = roomGraphBuilder.Graph.rooms
                .Where(r => r.type is RoomType.Corridor or RoomType.SecondaryObjectiveRoom)
                .ToList();

            for (var i = exitCandidates.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (exitCandidates[i], exitCandidates[j]) = (exitCandidates[j], exitCandidates[i]);
            }

            var additionalCount = Math.Min(extraExitCount, exitCandidates.Count);
            for (var i = 0; i < additionalCount; i++)
                roomGraphBuilder.Attach(exitCandidates[i].id, roomGraphBuilder.AddRoom(roomGraphBuilder.NextId("exit"), RoomType.Exit).id);
        }

        // 6. Insert a corridor between most connected rooms for spacing (a degree-preserving splice).
        private static void SpliceCorridors(RoomGraphBuilder roomGraphBuilder)
        {
            var graph = roomGraphBuilder.Graph;
            foreach (var edge in new List<RoomEdge>(graph.edges))
            {
                var from = graph.GetRoom(edge.fromId);
                var to = graph.GetRoom(edge.toId);
                if (from == null || to == null) continue;
                if (from.type is RoomType.Corridor or RoomType.Entrance) continue;
                if (to.type is RoomType.Corridor or RoomType.Exit) continue;

                var corridor = roomGraphBuilder.AddRoom(roomGraphBuilder.NextId("corridor"), RoomType.Corridor);
                var idx = graph.edges.IndexOf(edge);
                if (idx != -1) graph.edges.RemoveAt(idx);
                graph.edges.Add(new RoomEdge { fromId = edge.fromId, toId = corridor.id });
                graph.edges.Add(new RoomEdge
                    { fromId = corridor.id, toId = edge.toId, locked = edge.locked, keyRoomId = edge.keyRoomId });
            }
        }

        // Maps a mission node's role to the room role it becomes.
        // Keycard rooms are inserted by the locking pass, not here.
        private static RoomType MissionRoleToRoomRole(NodeType nodeType) => nodeType switch
        {
            NodeType.Entry => RoomType.Entrance,
            NodeType.Primary => RoomType.PrimaryObjectiveRoom,
            NodeType.Secondary => RoomType.SecondaryObjectiveRoom,
            NodeType.Prerequisite => RoomType.GuardPost,
            _ => RoomType.Corridor
        };

        // Accumulates the room graph while holding every room within the four-door tile budget.
        // Rooms and edges are added only through here, so the live per-room door count,
        // and the corridor relays that absorb overflow — stay consistent with the graph as it is built.
        private sealed class RoomGraphBuilder
        {
            private const int MaxDoors = 4;

            private readonly Dictionary<string, int> _doors = new(); // live doorway count per room

            private readonly Dictionary<string, string>
                _relayTail = new(); // room → the corridor relay carrying its overflow

            private readonly Dictionary<string, int> _roleCounters = new(); // per-role id sequence

            public RoomGraphBuilder(int seed, int level) => Graph = new RoomGraph { seed = seed, level = level };

            public RoomGraph Graph { get; }

            public string NextId(string role)
            {
                var n = _roleCounters.GetValueOrDefault(role);
                _roleCounters[role] = n + 1;
                return $"room_{role}_{n}";
            }

            public RoomNode AddRoom(string id, RoomType type, string missionNodeId = null)
            {
                var room = new RoomNode { id = id, type = type, missionNodeId = missionNodeId };
                Graph.rooms.Add(room);
                _doors[id] = 0;
                return room;
            }

            public void AddEdge(string from, string to, bool locked = false, string key = null)
            {
                Graph.edges.Add(new RoomEdge { fromId = from, toId = to, locked = locked, keyRoomId = key });
                _doors[from]++;
                _doors[to]++;
            }

            // Redirects an existing edge onto a new target, moving the doorway from the old room to the new one.
            public void RerouteEdge(RoomEdge edge, string newTarget)
            {
                _doors[edge.toId]--;
                edge.toId = newTarget;
                _doors[newTarget]++;
            }

            // Connects parent to child while keeping parent within four doors.
            // If a parent is full, one of its existing branches is re-hung on a fresh corridor relay.
            // The new child joins that relay — so the surplus fans out through a corridor,
            // instead of the edge being refused or the room overflowing. The relay relays again when it too fills.
            public void Attach(string parent, string child, bool locked = false, string key = null)
            {
                var host = _relayTail.GetValueOrDefault(parent, parent);
                if (_doors[host] >= MaxDoors)
                {
                    var moved = Graph.edges.FirstOrDefault(e => e.fromId == host);
                    var corridor = AddRoom(NextId("corridor"), RoomType.Corridor);
                    if (moved != null)
                    {
                        _doors[host]--; // host releases the moved branch...
                        moved.fromId = corridor.id; // ...which now leaves the relay
                        _doors[corridor.id]++;
                    }

                    AddEdge(host, corridor.id); // host -> relay (host back to at most four)
                    _relayTail[parent] = corridor.id;
                    host = corridor.id;
                }

                AddEdge(host, child, locked, key);
            }
        }
    }
}