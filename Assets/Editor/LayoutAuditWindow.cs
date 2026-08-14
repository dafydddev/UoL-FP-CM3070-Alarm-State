using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    // Visual front-end for LayoutAudit (Tools -> Layout Audit).
    // Renders the audit results as per-profile pass/fail cards with colour-coded check tables.
    public class LayoutAuditWindow : EditorWindow
    {
        private int _seeds = LayoutAudit.QuickSeeds;
        private bool _includeStress;
        private LayoutAudit.AuditResult _result; // most recent completed run, or null
        private Vector2 _scroll;

        // Status colours (green/red/gold match GraphEditorWindow's node palette).
        private static readonly Color ColPass = new(0.13f, 0.77f, 0.37f); // green - check passed
        private static readonly Color ColFail = new(0.94f, 0.27f, 0.27f); // red - structural violation
        private static readonly Color ColInfo = new(0.92f, 0.70f, 0.03f); // gold - informational, not a violation
        private static readonly Color ColMuted = new(0.6f, 0.6f, 0.7f); // secondary text
        private static readonly Color ColBannerPass = new(0.09f, 0.23f, 0.14f); // banner fill behind PASS
        private static readonly Color ColBannerFail = new(0.28f, 0.10f, 0.10f); // banner fill behind FAIL
        private static readonly Color ColZebra = new(1f, 1f, 1f, 0.035f); // alternate-row tint

        // Layout metrics.
        private const float BannerHeight = 40f;
        private const float BannerPadding = 10f;
        private const float PillWidth = 44f; // per-profile PASS/FAIL badge
        private const float PillHeight = 16f;
        private const float PillYNudge = 2f; // optical alignment against the larger title text
        private const float CheckColWidth = 170f; // first table column (check name)
        private const float ValueColWidth = 150f; // spine / walk value columns
        private const int TitleFontSize = 14; // profile card titles
        private const int BannerFontSize = 15; // PASS/FAIL headline

        [MenuItem("Tools/Layout Audit")]
        public static void Open() => GetWindow<LayoutAuditWindow>("Layout Audit");

        private void OnGUI()
        {
            DrawControls();

            if (_result == null)
            {
                EditorGUILayout.HelpBox(
                    "Run the audit to exercise the full generation pipeline over many seeds, " +
                    "levels and difficulty profiles, and check every layout for structural violations.",
                    MessageType.Info);
                return;
            }

            DrawBanner();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var profile in _result.Profiles) DrawProfile(profile);
            EditorGUILayout.EndScrollView();
        }

        // Seed count, stress toggle, and the run buttons (custom + the two presets).
        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Audit", EditorStyles.boldLabel);
            _seeds = Mathf.Max(1, EditorGUILayout.IntField("Seeds", _seeds));
            _includeStress = EditorGUILayout.Toggle("Include Stress Profile", _includeStress);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run")) ScheduleRun();
            if (GUILayout.Button($"Quick ({LayoutAudit.QuickSeeds} seeds)"))
            {
                _seeds = LayoutAudit.QuickSeeds;
                _includeStress = false;
                ScheduleRun();
            }

            if (GUILayout.Button($"Thorough ({LayoutAudit.ThoroughSeeds} seeds + stress)"))
            {
                _seeds = LayoutAudit.ThoroughSeeds;
                _includeStress = true;
                ScheduleRun();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // Defers the audit until after OnGUI, avoid race condition with Unity's GUI.
        private void ScheduleRun()
        {
            var seeds = _seeds;
            var stress = _includeStress;
            EditorApplication.delayCall += () =>
            {
                var result = LayoutAudit.RunAudit(seeds, stress);
                if (result != null) _result = result; // null = cancelled; keep the previous run
                Repaint();
            };
        }

        // Full-width PASS/FAIL strip with the run's shape and timing on the right.
        private void DrawBanner()
        {
            var pass = !_result.AnyViolation;
            EditorGUILayout.Space(2);
            var rect = GUILayoutUtility.GetRect(0, BannerHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, pass ? ColBannerPass : ColBannerFail);

            var headline = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = BannerFontSize,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = pass ? ColPass : ColFail }
            };
            var detail = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = ColMuted }
            };

            var failing = _result.Profiles.Count(p => p.HasViolation);
            var resultTitle = pass
                ? "PASS — no structural violations"
                : $"FAIL — violations in {failing} of {_result.Profiles.Count} profiles";
            GUI.Label(new Rect(rect.x + BannerPadding, rect.y, rect.width * 0.6f, rect.height), resultTitle, headline);

            var meta =
                $"{_result.Seeds} seeds × levels 1..{LayoutAudit.TotalLevels} × {_result.Profiles.Count} profiles × 2 styles" +
                $"  ·  {_result.DurationSeconds:F1}s  ·  {_result.CompletedAt:HH:mm}";
            GUI.Label(new Rect(rect.x, rect.y, rect.width - BannerPadding, rect.height), meta, detail);
            EditorGUILayout.Space(2);
        }

        // One card per difficulty profile.
        // Header with pass/fail pill, graph checks, and a table of the layout checks.
        private static void DrawProfile(LayoutAudit.ProfileResult p)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            DrawPill(p.HasViolation);
            GUILayout.Label(p.Name, new GUIStyle(EditorStyles.boldLabel) { fontSize = TitleFontSize });
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{p.Graph.Levels:N0} levels  ·  avg {p.AvgRooms:F1} rooms  ·  max {p.Graph.MaxRooms}",
                MutedStyle());
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Room graph", EditorStyles.miniBoldLabel);
            var zebra = false;
            StatRow("Multi-parent rooms", p.Graph.MultiParent, ref zebra);
            StatRow("Door-budget breaches", p.Graph.OverBudget, ref zebra);
            StatRow("Unreachable rooms", p.Graph.Unreachable, ref zebra);

            EditorGUILayout.Space(4);
            GUILayout.Label("Tile layout", EditorStyles.miniBoldLabel);
            DrawStyleHeader();
            zebra = false;
            LayoutRow("Stacked rooms",
                LevelsWith(p.Spine.StackedLevels, p.Spine.StackedCells, "cells"), p.Spine.StackedLevels > 0,
                LevelsWith(p.Walk.StackedLevels, p.Walk.StackedCells, "cells"), p.Walk.StackedLevels > 0,
                info: false, ref zebra);
            LayoutRow("Non-adjacent doors",
                LevelsWith(p.Spine.NonAdjLevels, p.Spine.NonAdjEdges, "edges"), p.Spine.NonAdjLevels > 0,
                LevelsWith(p.Walk.NonAdjLevels, p.Walk.NonAdjEdges, "edges"), p.Walk.NonAdjLevels > 0,
                info: false, ref zebra);
            LayoutRow("Missing rooms",
                p.Spine.Missing.ToString("N0"), p.Spine.Missing > 0,
                p.Walk.Missing.ToString("N0"), p.Walk.Missing > 0,
                info: false, ref zebra);
            LayoutRow("Relief corridors",
                Relief(p.Spine), p.Spine.ReliefLevels > 0,
                Relief(p.Walk), p.Walk.ReliefLevels > 0,
                info: true, ref zebra);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // Column headings for the per-style table, aligned with the value columns below.
        private static void DrawStyleHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(string.Empty, GUILayout.Width(CheckColWidth));
            var style = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = ColMuted } };
            GUILayout.Label("Spine", style, GUILayout.Width(ValueColWidth));
            GUILayout.Label("Random walk", style, GUILayout.Width(ValueColWidth));
            EditorGUILayout.EndHorizontal();
        }

        // A single-value check row (the graph checks apply to both styles equally).
        private static void StatRow(string label, long value, ref bool zebra)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint && zebra) EditorGUI.DrawRect(rect, ColZebra);
            zebra = !zebra;
            GUILayout.Label(label, GUILayout.Width(CheckColWidth));
            GUILayout.Label(value.ToString("N0"), ValueStyle(value > 0, info: false), GUILayout.Width(ValueColWidth));
            EditorGUILayout.EndHorizontal();
        }

        // A check row with one value per layout style.
        private static void LayoutRow(string label, string spineText, bool spineBad, string walkText, bool walkBad,
            bool info, ref bool zebra)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint && zebra) EditorGUI.DrawRect(rect, ColZebra);
            zebra = !zebra;
            GUILayout.Label(label, GUILayout.Width(CheckColWidth));
            GUILayout.Label(spineText, ValueStyle(spineBad, info), GUILayout.Width(ValueColWidth));
            GUILayout.Label(walkText, ValueStyle(walkBad, info), GUILayout.Width(ValueColWidth));
            EditorGUILayout.EndHorizontal();
        }

        // Green for a clean check, bold red for a violation; informational rows use gold.
        private static GUIStyle ValueStyle(bool flagged, bool info)
        {
            var colour = info
                ? flagged ? ColInfo : ColMuted
                : flagged
                    ? ColFail
                    : ColPass;
            return new GUIStyle(EditorStyles.label)
            {
                fontStyle = flagged && !info ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = colour }
            };
        }

        private static GUIStyle MutedStyle() => new(EditorStyles.label) { normal = { textColor = ColMuted } };

        // "3 levels (7 cells)" once a violation shows up, plain "0" otherwise.
        private static string LevelsWith(long levels, long extra, string unit) =>
            levels == 0 ? "0" : $"{levels:N0} levels ({extra:N0} {unit})";

        private static string Relief(LayoutAudit.Stats s) =>
            s.ReliefLevels == 0 ? "0" : $"{s.ReliefLevels:N0} levels (+{s.ReliefRooms:N0} rooms)";

        // Small PASS/FAIL badge in each profile card's header.
        private static void DrawPill(bool fail)
        {
            var rect = GUILayoutUtility.GetRect(PillWidth, PillHeight, GUILayout.Width(PillWidth));
            rect.y += PillYNudge;
            EditorGUI.DrawRect(rect, fail ? ColFail : ColPass);
            GUI.Label(rect, fail ? "FAIL" : "PASS", new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }
    }
}