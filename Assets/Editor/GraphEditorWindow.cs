#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Generation;
using Graphs;
using UnityEditor;
using UnityEngine;
using Graphs.Missions;
using Graphs.Rooms;
using Run;

namespace Editor
{
    // Editor tool (Tools -> Mission Graph Editor) for previewing generation without entering play mode.
    // Generates a mission + room graph from the chosen settings and draws both as interactive diagrams.
    public class GraphEditorWindow : EditorWindow
    {
        // Generation settings, mirroring the runtime generators.
        private RunDifficulty _runDifficulty;
        private int _level = 1;
        private int _totalLevels = 20; // the run length the player chose (10/20/30)
        private MissionType _forcedType = MissionType.Assassination;
        private bool _randomType = true;
        private int _seed = 1;
        private bool _randomSeed = true;

        // How well the player has been doing, -1 struggling to +1 thriving.
        private float _standing;

        // The most recently generated graphs.
        private MissionGraph _missionGraph;
        private RoomGraph _roomGraph;

        // Which graph the window is currently showing.
        private enum EditorTab
        {
            Mission,
            Room
        }

        private EditorTab _editorTab = EditorTab.Mission;
        private Vector2 _scroll; // scroll position of the graph canvas
        private float _zoom = 1f; // current zoom factor applied to the graph canvas
        private string _selectedNodeId; // id of the node shown in the inspector, or null

        // Computed screen positions for each node, per graph.
        private readonly Dictionary<string, Vector2> _missionPositions = new();
        private readonly Dictionary<string, Vector2> _roomPositions = new();

        // Formatting Styles
        private const int FontSize = 12; // Standard text size
        private const int FontSizeSmall = 10; // Smaller text size

        // Node and layout dimensions.
        private const float NodeW = 140f; // node box width
        private const float NodeH = 50f; // node box height
        private const float NodeGapX = 40f; // horizontal gap between node edges at adjacent levels
        private const float NodeGapY = 30f; // vertical gap between node edges within the same level
        private const float LevelSpacingX = NodeW + NodeGapX; // distance between level centres
        private const float LevelSpacingY = NodeH + NodeGapY; // distance between stacked node centres
        private const float CanvasOffsetX = 20f; // margin before the first level
        private const float CanvasOffsetY = 20f; // margin before the first row

        // Zoom limits.
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 3f;
        private const float ScrollZoomSensitivity = 0.05f; // zoom change per scroll-wheel unit

        // Generation slider range for the player-progression level.
        private const int MinLevel = 0;

        // Meta-bar column widths.
        private const float MetaFacilityWidth = 220f;
        private const float MetaTypeWidth = 150f;
        private const float MetaSeedWidth = 120f;
        private const float MetaNodesWidth = 80f;

        // Tab bar widths.
        private const float ZoomLabelWidth = 80f;
        private const float ZoomSliderWidth = 120f;
        private const float ResetButtonWidth = 50f;

        // Space reserved below the canvas for settings/meta/tabs/inspector.
        private const float BottomChromeHeight = 220f;

        // How many down/up barycenter sweeps to run when ordering nodes within each level.
        private const int OrderingPasses = 4;

        // Node box rendering.
        private const float NodeFillDarken = 0.25f; // how much to darken a node's colour for its fill
        private const float NodePadding = 4f;
        private const float TitleHeightRatio = 0.6f;
        private const float SubtitleTopRatio = 0.55f;
        private const float SubtitleHeightRatio = 0.4f;

        // Halo ring drawn around the primary objective's node.
        private const float HaloInflate = 4f; // gap between the node box and its halo ring
        private const float HaloThickness = 2f; // halo ring thickness

        // Arrow drawing.
        private const float DashLength = 6f;
        private const float ArrowHeadLength = 10f;
        private const float ArrowHeadWidth = 5f;

        // Node/edge colours by type.
        private static readonly Color ColEntry = new(0.23f, 0.51f, 0.96f); // blue
        private static readonly Color ColPrereq = new(0.66f, 0.33f, 0.97f); // purple
        private static readonly Color ColPrimary = new(0.94f, 0.27f, 0.27f); // red
        private static readonly Color ColSecondary = new(0.13f, 0.77f, 0.37f); // green
        private static readonly Color ColEdge = new(0.28f, 0.33f, 0.41f); // slate
        private static readonly Color ColEdgeLocked = new(0.92f, 0.70f, 0.03f); // gold
        private static readonly Color ColBackground = new(0.08f, 0.09f, 0.11f);

        // Registers the window under the Tools menu.
        [MenuItem("Tools/Mission Graph Editor")]
        public static void Open() => GetWindow<GraphEditorWindow>("Mission Graph Editor");

        // Main draw loop: settings on top, then (once generated) metadata, tabs, graph, and inspector.
        private void OnGUI()
        {
            DrawSettings();
            if (_missionGraph == null)
            {
                EditorGUILayout.HelpBox("Configure settings and press Generate.", MessageType.Info);
                return;
            }

            DrawMeta();
            DrawTabs();
            DrawGraph();
            DrawInspector();
        }

        // Draws the settings panel and, on Generate, builds both graphs and lays them out.
        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

            _runDifficulty = (RunDifficulty)EditorGUILayout.ObjectField("Difficulty Profile", _runDifficulty,
                typeof(RunDifficulty), false);
            _totalLevels = EditorGUILayout.IntPopup("Total Levels", _totalLevels, new[] { "10", "20", "30" },
                new[] { 10, 20, 30 });
            _level = EditorGUILayout.IntSlider("Level", _level, MinLevel, _totalLevels);
            _randomType = EditorGUILayout.Toggle("Random Type", _randomType);
            if (!_randomType)
            {
                _forcedType = (MissionType)EditorGUILayout.EnumPopup("Mission Type", _forcedType);
            }

            _randomSeed = EditorGUILayout.Toggle("Random Seed", _randomSeed);
            if (!_randomSeed)
            {
                _seed = EditorGUILayout.IntField("Seed", _seed);
            }

            _standing = EditorGUILayout.Slider("Standing", _standing, -1f, 1f);

            if (!_runDifficulty)
            {
                EditorGUILayout.HelpBox("Assign a Difficulty Profile before generating.", MessageType.Warning);
            }
            else if (GUILayout.Button("Generate"))
            {
                // Run the (editor copy of the) mission generator, then expand into a room graph.
                var gen = new MissionGeneratorRuntime
                {
                    Profile = _runDifficulty,
                    ForcedType = _forcedType,
                    RandomType = _randomType,
                    Seed = _seed,
                    RandomSeed = _randomSeed
                };

                _missionGraph = gen.Generate(_level, _totalLevels);
                _roomGraph =
                    RoomGraphGenerator.Generate(_missionGraph, _runDifficulty, _level, _totalLevels, _standing);
                _seed = _missionGraph.seed; // reflect the used seed back into the field

                // Compute node positions for both diagrams.
                LayoutGraph(
                    _missionGraph.nodes.Select(n => n.id),
                    _missionGraph.nodes.SelectMany(n => n.dependencies.Select(d => (d, n.id))),
                    _missionPositions);

                LayoutGraph(
                    _roomGraph.rooms.Select(r => r.id),
                    _roomGraph.edges.Select(e => (e.fromId, e.toId)),
                    _roomPositions);

                _selectedNodeId = null;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        // Draws a one-line summary bar of the generated mission/room graph.
        private void DrawMeta()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Facility: {_missionGraph.facility}", GUILayout.Width(MetaFacilityWidth));
            EditorGUILayout.LabelField($"Type: {_missionGraph.type}", GUILayout.Width(MetaTypeWidth));
            EditorGUILayout.LabelField($"Seed: {_missionGraph.seed}", GUILayout.Width(MetaSeedWidth));
            EditorGUILayout.LabelField($"Nodes: {_missionGraph.nodes.Count}", GUILayout.Width(MetaNodesWidth));
            EditorGUILayout.LabelField($"Rooms: {_roomGraph.rooms.Count}");
            EditorGUILayout.LabelField($"Exits: {_roomGraph.rooms.Count(r => r.type == RoomType.Exit)}");
            EditorGUILayout.LabelField($"Supply: {_roomGraph.rooms.Count(r => r.type == RoomType.SupplyRoom)}");
            EditorGUILayout.LabelField($"Pressure: {_roomGraph.rooms.Count(r => r.type == RoomType.PressureRoom)}");
            EditorGUILayout.EndHorizontal();
        }

        // Mission/Room toggle buttons, plus the zoom controls.
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_editorTab == EditorTab.Mission, "Mission Graph", EditorStyles.toolbarButton))
                _editorTab = EditorTab.Mission;
            if (GUILayout.Toggle(_editorTab == EditorTab.Room, "Room Graph", EditorStyles.toolbarButton))
                _editorTab = EditorTab.Room;
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Zoom {_zoom:0.00}x", GUILayout.Width(ZoomLabelWidth));
            _zoom = GUILayout.HorizontalSlider(_zoom, MinZoom, MaxZoom, GUILayout.Width(ZoomSliderWidth));
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(ResetButtonWidth))) _zoom = 1f;
            EditorGUILayout.EndHorizontal();
        }

        // Draws the scrollable graph canvas for whichever tab is active.
        private void DrawGraph()
        {
            var canvasRect = GUILayoutUtility.GetRect(position.width, position.height - BottomChromeHeight);
            EditorGUI.DrawRect(canvasRect, ColBackground);

            // Scroll-wheel over the canvas zooms in/out.
            if (Event.current.type == EventType.ScrollWheel && canvasRect.Contains(Event.current.mousePosition))
            {
                _zoom = Mathf.Clamp(_zoom - Event.current.delta.y * ScrollZoomSensitivity, MinZoom, MaxZoom);
                Event.current.Use();
                Repaint();
            }

            _scroll = GUI.BeginScrollView(canvasRect, _scroll, ComputeContentRect());

            if (_editorTab == EditorTab.Mission) DrawMissionGraph();
            else DrawRoomGraph();

            GUI.EndScrollView();
        }

        // Draws the mission graph: dependency arrows first, then coloured nodes on top.
        private void DrawMissionGraph()
        {
            foreach (var node in _missionGraph.nodes)
            {
                if (!_missionPositions.TryGetValue(node.id, out var toPos)) continue;
                foreach (var dep in node.dependencies)
                {
                    if (!_missionPositions.TryGetValue(dep, out var fromPos)) continue;
                    DrawArrow(fromPos * _zoom, toPos * _zoom, ColEdge, false, _zoom);
                }
            }

            foreach (var node in _missionGraph.nodes)
            {
                if (!_missionPositions.TryGetValue(node.id, out var pos)) continue;
                var col = node.nodeType switch
                {
                    NodeType.Entry => ColEntry,
                    NodeType.Prerequisite => ColPrereq,
                    NodeType.Primary => ColPrimary,
                    NodeType.Secondary => ColSecondary,
                    _ => Color.grey
                };
                DrawNode(pos * _zoom, node.id, node.text, node.label, col, node.nodeType == NodeType.Primary);
            }
        }

        // Draws the room graph: connection arrows (dashed if locked), then coloured room nodes.
        private void DrawRoomGraph()
        {
            foreach (var edge in _roomGraph.edges)
            {
                if (!_roomPositions.TryGetValue(edge.fromId, out var fromPos)) continue;
                if (!_roomPositions.TryGetValue(edge.toId, out var toPos)) continue;
                DrawArrow(fromPos * _zoom, toPos * _zoom, edge.locked ? ColEdgeLocked : ColEdge, edge.locked, _zoom);
            }

            foreach (var room in _roomGraph.rooms)
            {
                if (!_roomPositions.TryGetValue(room.id, out var pos)) continue;
                // Rooms draw in the same colours the game tints them, from the shared table.
                var col = RoomColour.TryFor(room.type, out var roleColour) ? roleColour : Color.grey;
                // Show the underlying mission text where there is one, else just the type.
                var label = room.missionNodeId != null
                    ? _missionGraph.nodes.Find(n => n.id == room.missionNodeId)?.text ?? room.type.ToString()
                    : room.type.ToString();
                DrawNode(pos * _zoom, room.id, label, room.type.ToString(), col,
                    room.type == RoomType.PrimaryObjectiveRoom);
            }
        }

        // Draws details for the currently selected mission node or room.
        private void DrawInspector()
        {
            if (_selectedNodeId == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);

            if (_editorTab == EditorTab.Mission)
            {
                var node = _missionGraph.nodes.Find(n => n.id == _selectedNodeId);
                if (node != null)
                {
                    EditorGUILayout.LabelField("Text", node.text);
                    EditorGUILayout.LabelField("Label", node.label);
                    EditorGUILayout.LabelField("Type", node.nodeType.ToString());
                    EditorGUILayout.LabelField("ID", node.id);
                    if (node.dependencies.Count > 0)
                    {
                        EditorGUILayout.LabelField("Dependencies", string.Join(", ", node.dependencies));
                    }
                }
            }
            else
            {
                var room = _roomGraph.rooms.Find(r => r.id == _selectedNodeId);
                if (room != null)
                {
                    EditorGUILayout.LabelField("Role", room.type.ToString());
                    EditorGUILayout.LabelField("ID", room.id);
                    EditorGUILayout.LabelField("Mission Node ID", room.missionNodeId ?? "—");
                    // List any locked edges leading into this room.
                    var locked = _roomGraph.edges.FindAll(e => e.toId == room.id && e.locked);
                    foreach (var e in locked)
                    {
                        EditorGUILayout.LabelField("Locked from", e.fromId);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // Assigns each node an (x, y) position using a layered layout:
        // a topological pass puts each node at a "level" = its longest dependency depth, nodes within each
        // level are ordered to reduce edge crossings (barycenter sweeps), and columns are vertically centred.
        private static void LayoutGraph(
            IEnumerable<string> ids,
            IEnumerable<(string from, string to)> edges,
            Dictionary<string, Vector2> positions)
        {
            positions.Clear();
            var idList = ids.ToList();
            var levels = new Dictionary<string, int>();
            var inDegree = new Dictionary<string, int>();
            var adj = new Dictionary<string, List<string>>();
            var preds = new Dictionary<string, List<string>>();

            // Initialise adjacency and in-degree for every node.
            foreach (var id in idList)
            {
                inDegree[id] = 0;
                adj[id] = new List<string>();
                preds[id] = new List<string>();
            }

            // Build the edge lists and in-degree counts.
            foreach (var (from, to) in edges)
            {
                if (!adj.ContainsKey(from)) adj[from] = new List<string>();
                if (!preds.ContainsKey(to)) preds[to] = new List<string>();
                inDegree.TryAdd(to, 0);
                adj[from].Add(to);
                preds[to].Add(from);
                inDegree[to]++;
            }

            // Recording each node's longest-path level.
            var queue = new Queue<string>();
            foreach (var id in idList)
                if (inDegree.TryGetValue(id, out var deg) && deg == 0)
                {
                    queue.Enqueue(id);
                    levels[id] = 0;
                }

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!adj.TryGetValue(id, out var neighbours)) continue;
                foreach (var nb in neighbours)
                {
                    var lvl = (levels.GetValueOrDefault(id, 0)) + 1;
                    if (!levels.TryGetValue(nb, out var cur) || lvl > cur) levels[nb] = lvl; // keep the deepest
                    if (--inDegree[nb] == 0) queue.Enqueue(nb);
                }
            }

            // Any node not reached (e.g. in a cycle) defaults to level 0.
            foreach (var id in idList) levels.TryAdd(id, 0);

            // Group nodes by level, keeping levels in left-to-right order for the sweeps below.
            var byLevel = new SortedDictionary<int, List<string>>();
            foreach (var id in idList)
            {
                var l = levels[id];
                if (!byLevel.ContainsKey(l)) byLevel[l] = new List<string>();
                byLevel[l].Add(id);
            }

            // Track each node's current row within its level so neighbours can be averaged.
            var rows = new Dictionary<string, float>();
            foreach (var group in byLevel.Values)
                for (var i = 0; i < group.Count; i++)
                    rows[group[i]] = i;

            // Barycenter ordering: alternate downward sweeps (align each node with its predecessors)
            // and upward sweeps (align with its successors) to reduce edge crossings.
            for (var pass = 0; pass < OrderingPasses; pass++)
            {
                SortLevelsByBarycenter(byLevel.Values, preds, rows);
                SortLevelsByBarycenter(byLevel.Values.Reverse(), adj, rows);
            }

            // Place each level in its own column,
            // stacking its nodes vertically and centring shorter columns against the tallest one.
            var tallest = 0;
            foreach (var group in byLevel.Values) tallest = Mathf.Max(tallest, group.Count);

            foreach (var (level, group) in byLevel)
            {
                var x = CanvasOffsetX + level * LevelSpacingX + NodeW * 0.5f;
                var yStart = CanvasOffsetY + NodeH * 0.5f + (tallest - group.Count) * LevelSpacingY * 0.5f;
                for (var i = 0; i < group.Count; i++)
                    positions[group[i]] = new Vector2(x, yStart + i * LevelSpacingY);
            }
        }

        // One barycenter sweep: reorders every level so each node sits near the average row of its
        // linked neighbours (predecessors or successors, depending on the sweep direction).
        // Nodes with no neighbours keep their current row; OrderBy is stable, so ties keep their order.
        private static void SortLevelsByBarycenter(
            IEnumerable<List<string>> levelGroups,
            Dictionary<string, List<string>> neighbours,
            Dictionary<string, float> rows)
        {
            foreach (var group in levelGroups)
            {
                var ordered = group.OrderBy(id => Barycenter(id, neighbours, rows)).ToList();
                group.Clear();
                group.AddRange(ordered);
                for (var i = 0; i < group.Count; i++) rows[group[i]] = i;
            }
        }

        // Average row of a node's neighbours, or its own row when it has none to follow.
        private static float Barycenter(string id, Dictionary<string, List<string>> neighbours,
            Dictionary<string, float> rows)
        {
            if (!neighbours.TryGetValue(id, out var linked) || linked.Count == 0) return rows[id];
            var sum = 0f;
            var count = 0;
            foreach (var n in linked)
            {
                if (!rows.TryGetValue(n, out var r)) continue;
                sum += r;
                count++;
            }

            return count > 0 ? sum / count : rows[id];
        }

        // Computes the scroll-view content size needed to fit all nodes of the active tab.
        private Rect ComputeContentRect()
        {
            var positions = _editorTab == EditorTab.Mission ? _missionPositions : _roomPositions;
            var maxX = 0f;
            var maxY = 0f;
            foreach (var p in positions.Values)
            {
                maxX = Mathf.Max(maxX, p.x);
                maxY = Mathf.Max(maxY, p.y);
            }

            return new Rect(0, 0, (maxX + NodeW) * _zoom, (maxY + NodeH) * _zoom);
        }

        // Draws a single node box (title + subtitle) and handles click-to-select/deselect.
        // Highlight adds a halo ring around the box, marking the primary objective.
        private void DrawNode(Vector2 pos, string id, string text, string subtitle, Color col, bool highlight = false)
        {
            var w = NodeW * _zoom;
            var h = NodeH * _zoom;
            var rect = new Rect(pos.x - w * 0.5f, pos.y - h * 0.5f, w, h);
            var isSelected = _selectedNodeId == id;
            var bg = col * NodeFillDarken; // darkened fill
            bg.a = 1f;

            EditorGUI.DrawRect(rect, bg);
            DrawBorder(rect, isSelected ? Color.white : col, isSelected ? 2f : 1f); // highlight when selected

            if (highlight)
            {
                var inflate = HaloInflate * _zoom;
                var halo = new Rect(rect.x - inflate, rect.y - inflate, rect.width + inflate * 2f,
                    rect.height + inflate * 2f);
                DrawBorder(halo, col, HaloThickness);
            }

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperCenter, fontSize = Mathf.Max(1, Mathf.RoundToInt(FontSize * _zoom)),
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            var subStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.LowerCenter, fontSize = Mathf.Max(1, Mathf.RoundToInt(FontSizeSmall * _zoom)),
                normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }
            };

            GUI.Label(
                new Rect(rect.x + NodePadding, rect.y + NodePadding, rect.width - NodePadding * 2,
                    rect.height * TitleHeightRatio), text, labelStyle);
            GUI.Label(
                new Rect(rect.x + NodePadding, rect.y + rect.height * SubtitleTopRatio, rect.width - NodePadding * 2,
                    rect.height * SubtitleHeightRatio), subtitle,
                subStyle);

            // Toggle selection if this node was clicked.
            if (Event.current.type != EventType.MouseDown || !rect.Contains(Event.current.mousePosition)) return;
            _selectedNodeId = isSelected ? null : id;
            Event.current.Use();
            Repaint();
        }

        // Finds where a ray from a node's centre exits its box, so arrows touch the edge, not the middle.
        private static Vector2 RectEdgeIntersect(Vector2 centre, Vector2 dir, float w, float h)
        {
            var hw = w * 0.5f;
            var hh = h * 0.5f;
            var tx = dir.x != 0 ? hw / Mathf.Abs(dir.x) : float.MaxValue;
            var ty = dir.y != 0 ? hh / Mathf.Abs(dir.y) : float.MaxValue;
            return centre + dir * Mathf.Min(tx, ty);
        }

        // Draws an arrow between two node boxes, optionally dashed (for locked edges), with a head.
        private static void DrawArrow(Vector2 from, Vector2 to, Color col, bool dashed, float zoom)
        {
            var dir = (to - from).normalized;
            var start = RectEdgeIntersect(from, dir, NodeW * zoom, NodeH * zoom); // exit point on the source box
            var end = RectEdgeIntersect(to, -dir, NodeW * zoom, NodeH * zoom); // entry point on the target box
            var old = Handles.color;
            Handles.color = col;

            if (dashed)
            {
                // Walk the line in fixed-length segments, drawing every other one.
                var dashLen = DashLength * zoom;
                var total = Vector2.Distance(start, end);
                var drawn = 0f;
                var on = true;
                while (drawn < total)
                {
                    var seg = Mathf.Min(dashLen, total - drawn);
                    var s = Vector2.Lerp(start, end, drawn / total);
                    var e = Vector2.Lerp(start, end, (drawn + seg) / total);
                    if (on) Handles.DrawLine(s, e);
                    drawn += seg;
                    on = !on;
                }
            }
            else
            {
                Handles.DrawLine(start, end);
            }

            // Draw the two-pronged arrowhead at the end.
            if ((end - start).sqrMagnitude > 0.01f)
            {
                var perp = new Vector2(-dir.y, dir.x) * (ArrowHeadWidth * zoom);
                Handles.DrawLine(end, end - dir * (ArrowHeadLength * zoom) + perp);
                Handles.DrawLine(end, end - dir * (ArrowHeadLength * zoom) - perp);
            }

            Handles.color = old;
        }

        // Draws a t-thick rectangle outline by filling its four edges.
        private static void DrawBorder(Rect r, Color col, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), col);
            EditorGUI.DrawRect(new Rect(r.x, r.y + r.height - t, r.width, t), col);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), col);
            EditorGUI.DrawRect(new Rect(r.x + r.width - t, r.y, t, r.height), col);
        }
    }

    // A standalone copy of the mission generator used by the editor window. Mirrors MissionGenerator's logic.
    public class MissionGeneratorRuntime
    {
        public RunDifficulty Profile;
        public MissionType ForcedType = MissionType.Assassination;
        public bool RandomType = true;
        public int Seed;
        public bool RandomSeed = true;

        private System.Random _rng;

        // Builds a mission graph; see MissionGenerator.Generate for the full explanation.
        public MissionGraph Generate(int level, int totalLevels)
        {
            // Resolve the seed, then mix in the level so the same seed still varies per level.
            var resolvedSeed = RandomSeed ? UnityEngine.Random.Range(0, int.MaxValue) : Seed;
            _rng = new System.Random(Seeds.For(resolvedSeed, Seeds.Mission, level));

            // Pick the mission type and pull its content. Length comes from the enum itself, not a hardcoded count.
            var type = RandomType ? (MissionType)_rng.Next(0, Enum.GetValues(typeof(MissionType)).Length) : ForcedType;
            var (prereqSets, secondaries, terminalText, terminalLabel) = MissionObjectives.Data[type];

            // Choose facility, prerequisite chain, and how many secondaries to add.
            var facility = Pick(MissionObjectives.Facilities);
            var prereqSet = Pick(prereqSets);
            var numSecondaries = Profile.SecondaryObjectiveCount(level, totalLevels, _rng);

            var graph = new MissionGraph { type = type, facility = facility, seed = resolvedSeed };

            // Entry node.
            var entry = MakeNode("entry", "Infiltrate facility", "Mission start", NodeType.Entry);
            graph.nodes.Add(entry);

            // Chain the prerequisites, each depending on the previous.
            var prevIds = new List<string> { entry.id };
            foreach (var d in prereqSet)
            {
                var node = MakeNode($"prereq_{graph.nodes.Count}", d.text, d.label, NodeType.Prerequisite);
                node.dependencies.AddRange(prevIds);
                graph.nodes.Add(node);
                prevIds = new List<string> { node.id };
            }

            // Primary objective depends on the last prerequisite.
            var terminal = MakeNode("primary", terminalText, terminalLabel, NodeType.Primary);
            terminal.dependencies.AddRange(prevIds);
            graph.nodes.Add(terminal);

            // Secondaries branch off the entry or any prerequisite.
            var branchCandidates = graph.nodes
                .FindAll(n => n.nodeType is NodeType.Entry or NodeType.Prerequisite)
                .ConvertAll(n => n.id);

            // Add the chosen number of unique secondaries.
            var pool = new List<NodeData>(secondaries);
            for (var i = 0; i < numSecondaries && pool.Count > 0; i++)
            {
                var idx = _rng.Next(pool.Count);
                var d = pool[idx];
                pool.RemoveAt(idx);
                var sec = MakeNode($"secondary_{i}", d.text, d.label, NodeType.Secondary);
                sec.dependencies.Add(branchCandidates[_rng.Next(branchCandidates.Count)]);
                graph.nodes.Add(sec);
            }

            return graph;
        }

        // Helper to construct a node.
        private static MissionNode MakeNode(string id, string text, string label, NodeType type) => new()
            { id = id, text = text, label = label, nodeType = type };

        // Picks a random array element using the seeded RNG.
        private T Pick<T>(T[] arr) => arr[_rng.Next(arr.Length)];
    }
}
#endif