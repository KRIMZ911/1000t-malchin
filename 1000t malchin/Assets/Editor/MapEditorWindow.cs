using System.Linq;
using UnityEditor;
using UnityEngine;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// A simple paint-the-grid map editor. Pick a LevelDefinition, choose a terrain "brush"
    /// from the palette (the assets made by Malchin > Create Terrain Palette), then click
    /// cells to paint terrain. Set the whole-map background here too. Row 0 (the base edge)
    /// is shown at the bottom, matching the battlefield.
    ///
    /// Menu:  Malchin > Map Editor
    /// </summary>
    public class MapEditorWindow : EditorWindow
    {
        private LevelDefinition level;
        private TerrainDefinition brush;
        private TerrainDefinition[] palette = new TerrainDefinition[0];
        private Vector2 scroll;

        [MenuItem("Malchin/Map Editor")]
        public static void Open() => GetWindow<MapEditorWindow>("Malchin Map Editor");

        void OnEnable() => LoadPalette();

        void LoadPalette()
        {
            palette = AssetDatabase.FindAssets("t:TerrainDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<TerrainDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null)
                .ToArray();
            if (brush == null && palette.Length > 0) brush = palette[0];
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            level = (LevelDefinition)EditorGUILayout.ObjectField("Level", level, typeof(LevelDefinition), false);
            if (GUILayout.Button("Reload Palette")) LoadPalette();

            if (palette.Length == 0)
                EditorGUILayout.HelpBox("No terrain found. Run Malchin > Create Terrain Palette first.", MessageType.Warning);
            if (level == null)
            {
                EditorGUILayout.HelpBox("Assign a LevelDefinition to edit its map. Create one via Malchin > Create Battle Level.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            level.background = (Sprite)EditorGUILayout.ObjectField("Map Background", level.background, typeof(Sprite), false);

            EditorGUI.BeginChangeCheck();
            int w = Mathf.Max(1, EditorGUILayout.IntField("Grid Width", level.gridWidth));
            int h = Mathf.Max(1, EditorGUILayout.IntField("Grid Height", level.gridHeight));
            if (EditorGUI.EndChangeCheck()) { level.gridWidth = w; level.gridHeight = h; }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Resize / Init Tile Map")) ResizeTiles();
                if (GUILayout.Button("Fill All With Brush")) FillAll(brush);
                if (GUILayout.Button("Save")) Save();
            }

            // Palette (brush) row
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            DrawPalette();

            // Grid
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Map  ({level.gridWidth} x {level.gridHeight})  — row 0 (base) at bottom", EditorStyles.boldLabel);
            EnsureTiles();
            DrawGrid();
        }

        void DrawPalette()
        {
            int perRow = Mathf.Max(1, (int)(position.width / 130f));
            for (int i = 0; i < palette.Length; i += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < i + perRow && j < palette.Length; j++)
                    {
                        var t = palette[j];
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = t.color;
                        string label = (t == brush ? "● " : "") + t.displayName;
                        if (GUILayout.Button(label, GUILayout.Height(26), GUILayout.Width(125))) brush = t;
                        GUI.backgroundColor = prev;
                    }
                }
            }
            EditorGUILayout.LabelField("Selected: " + (brush != null ? brush.displayName : "(none)"));
        }

        void DrawGrid()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int row = level.gridHeight - 1; row >= 0; row--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int col = 0; col < level.gridWidth; col++)
                    {
                        int idx = row * level.gridWidth + col;
                        var t = level.tiles[idx];
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = t != null ? t.color : new Color(0.3f, 0.3f, 0.3f);
                        string cap = t != null && !string.IsNullOrEmpty(t.id)
                            ? t.id.Substring(0, Mathf.Min(3, t.id.Length)) : "·";
                        if (GUILayout.Button(cap, GUILayout.Width(34), GUILayout.Height(28)))
                        {
                            level.tiles[idx] = brush;
                            EditorUtility.SetDirty(level);
                        }
                        GUI.backgroundColor = prev;
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void EnsureTiles()
        {
            if (level.tiles == null || level.tiles.Length != level.gridWidth * level.gridHeight)
                ResizeTiles();
        }

        void ResizeTiles()
        {
            var old = level.tiles;
            var n = new TerrainDefinition[level.gridWidth * level.gridHeight];
            if (old != null)
                for (int i = 0; i < n.Length && i < old.Length; i++) n[i] = old[i];
            level.tiles = n;
            EditorUtility.SetDirty(level);
        }

        void FillAll(TerrainDefinition t)
        {
            EnsureTiles();
            for (int i = 0; i < level.tiles.Length; i++) level.tiles[i] = t;
            EditorUtility.SetDirty(level);
        }

        void Save()
        {
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Debug.Log($"Malchin: saved map for '{level.levelName}'.");
        }
    }
}
