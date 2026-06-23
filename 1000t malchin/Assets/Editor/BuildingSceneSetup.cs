using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Malchin.Building;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click setup for the Phase 2 base-building slice. Creates the ger
    /// definitions, the EventSystem + raycasters needed for tap/drag, the
    /// BuildingManager, three draggable placeholder gers, and the upgrade panel.
    ///
    /// Run AFTER "Malchin > Setup Economy Scene".
    /// Menu:  Malchin > Setup Building Scene
    /// </summary>
    public static class BuildingSceneSetup
    {
        private const string DataFolder = "Assets/Data";

        [MenuItem("Malchin/Setup Building Scene")]
        public static void Setup()
        {
            EnsureFolder(DataFolder);

            // 1. Ger definitions ------------------------------------------------------
            var herding = CreateGerDef("GerHerding", "herding", "Herding Ger",
                new Color(0.30f, 0.70f, 0.35f), 3, 3, GerEffectType.GrowthMultiplier, "sheep",
                new List<GerLevel>
                {
                    Lvl(1.0f, "Base sheep growth"),
                    Lvl(1.5f, "+50% sheep growth", ("sheep", 20)),
                    Lvl(2.0f, "+100% sheep growth", ("sheep", 60)),
                    Lvl(3.0f, "+200% sheep growth", ("sheep", 150)),
                });

            var main = CreateGerDef("GerMain", "main", "Main Ger",
                new Color(0.85f, 0.32f, 0.30f), 4, 4, GerEffectType.CapMultiplier, "sheep",
                new List<GerLevel>
                {
                    Lvl(1.0f, "Base sheep cap"),
                    Lvl(1.5f, "+50% sheep cap", ("cattle", 5)),
                    Lvl(2.0f, "+100% sheep cap", ("cattle", 15)),
                });

            var ovoo = CreateGerDef("GerOvoo", "ovoo", "Ovoo (Shrine)",
                new Color(0.70f, 0.50f, 0.90f), 3, 3, GerEffectType.None, "",
                new List<GerLevel>
                {
                    Lvl(1.0f, "Summoning shrine — opens in the gacha phase."),
                });

            // 2. Input plumbing -------------------------------------------------------
            EnsureEventSystem();
            EnsurePhysics2DRaycaster();

            // 3. Grid -----------------------------------------------------------------
            var grid = GetOrAdd<GridManager>(FindOrCreate("GridManager"));
            grid.cols = 20; grid.rows = 20; grid.cellSize = 0.5f;
            EditorUtility.SetDirty(grid);
            FrameCameraToGrid(grid);

            // 4. BuildingManager ------------------------------------------------------
            var bm = GetOrAdd<BuildingManager>(FindOrCreate("BuildingManager"));

            // 5. Canvas + upgrade panel ----------------------------------------------
            var canvas = EnsureCanvas();
            bm.upgradePanel = BuildUpgradePanel(canvas.transform);
            EditorUtility.SetDirty(bm);

            // 6. Place the gers on the grid (default, non-overlapping cells) ----------
            CreateGer(main,    "main",    new Vector2Int(8, 11)); // 4x4
            CreateGer(herding, "herding", new Vector2Int(3, 4));  // 3x3
            CreateGer(ovoo,    "ovoo",    new Vector2Int(14, 4)); // 3x3

            // Save --------------------------------------------------------------------
            AssetDatabase.SaveAssets();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=lime>Malchin: building scene setup complete.</color> " +
                      "Press Play, then tap a ger to upgrade it or drag it to move it.");
        }

        // ── Ger definitions ──────────────────────────────────────────────────────

        private static GerLevel Lvl(float effect, string note, params (string id, int amount)[] costs)
        {
            var lvl = new GerLevel { effectValue = effect, note = note };
            foreach (var c in costs)
                lvl.costToReach.Add(new LivestockCost { livestockId = c.id, amount = c.amount });
            return lvl;
        }

        private static GerDefinition CreateGerDef(string assetName, string id, string display,
            Color color, int footW, int footH, GerEffectType effect, string target, List<GerLevel> levels)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<GerDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<GerDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = display;
            def.placeholderColor = color;
            def.footprintWidth = footW;
            def.footprintHeight = footH;
            def.effectType = effect;
            def.effectTargetLivestockId = target;
            def.levels = levels;
            EditorUtility.SetDirty(def);
            return def;
        }

        // ── Ger world objects ────────────────────────────────────────────────────

        private static void CreateGer(GerDefinition def, string instanceId, Vector2Int cell)
        {
            string goName = $"Ger_{instanceId}";
            var go = GameObject.Find(goName);
            if (go == null) go = new GameObject(goName);

            var view = GetOrAdd<GerView>(go);          // pulls in SpriteRenderer + BoxCollider2D
            view.definition = def;
            view.instanceId = instanceId;
            view.originCell = cell;

            var col = go.GetComponent<BoxCollider2D>();
            col.size = new Vector2(def.footprintWidth * 0.5f, def.footprintHeight * 0.5f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = def.placeholderColor;

            // Edit-time position; ConfigureForGrid() re-snaps exactly at runtime.
            go.transform.position = EditorBlockCenter(cell, def.footprintWidth, def.footprintHeight);
            EditorUtility.SetDirty(go);
        }

        // Mirrors GridManager math for edit-time placement (20x20 grid, 0.5 cell, centered).
        private static Vector3 EditorBlockCenter(Vector2Int origin, int w, int h)
        {
            const float cs = 0.5f; const int cols = 20, rows = 20;
            Vector3 o = new Vector3(-cols * cs / 2f, -rows * cs / 2f, 0f);
            return new Vector3(o.x + (origin.x + w / 2f) * cs, o.y + (origin.y + h / 2f) * cs, 0f);
        }

        private static void FrameCameraToGrid(GridManager grid)
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            // Fit the grid width in a portrait (9:16) viewport, with a little margin.
            float halfWidthNeeded = grid.GridWorldWidth / 2f + grid.cellSize;
            cam.orthographicSize = halfWidthNeeded / (9f / 16f);
            EditorUtility.SetDirty(cam);
        }

        // ── Input plumbing ───────────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                GetOrAdd<InputSystemUIInputModule>(es.gameObject);
            }
        }

        private static void EnsurePhysics2DRaycaster()
        {
            var cam = Camera.main;
            if (cam != null) GetOrAdd<Physics2DRaycaster>(cam.gameObject);
            else Debug.LogWarning("Malchin: no Main Camera found — taps on gers won't register until one exists.");
        }

        // ── Upgrade panel UI ─────────────────────────────────────────────────────

        private static BuildingUpgradePanel BuildUpgradePanel(Transform canvas)
        {
            // Rebuild fresh each run so layout changes always take effect.
            var existing = canvas.Find("BuildingUI");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Always-active host so the component's Start() runs.
            var host = new GameObject("BuildingUI", typeof(RectTransform));
            host.transform.SetParent(canvas, false);
            Stretch(host.GetComponent<RectTransform>());
            var panelComp = host.AddComponent<BuildingUpgradePanel>();

            // Toggled visual root.
            var root = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(host.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(0f, 0f);          // centered so nothing clips off-screen
            rrt.sizeDelta = new Vector2(760f, 560f);
            root.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f, 0.92f);

            var nameText  = CreateText("Name",  root.transform, "Ger",      46, new Vector2(0f, 200f),  new Vector2(720f, 70f), TextAlignmentOptions.Center);
            var levelText = CreateText("Level", root.transform, "Level 0",  32, new Vector2(0f, 120f),  new Vector2(720f, 50f), TextAlignmentOptions.Center);
            var costText  = CreateText("Cost",  root.transform, "Cost: -",  30, new Vector2(0f,  40f),  new Vector2(720f, 50f), TextAlignmentOptions.Center);

            var upgradeBtn = CreateButton("UpgradeButton", root.transform, "Upgrade",
                new Vector2(0f, -160f), new Vector2(380f, 120f), new Color(0.25f, 0.55f, 0.32f), out var upgradeLabel);

            var closeBtn = CreateButton("CloseButton", root.transform, "X",
                new Vector2(322f, 240f), new Vector2(64f, 64f), new Color(0.55f, 0.25f, 0.25f), out _);

            // Wire references
            panelComp.root = root;
            panelComp.nameText = nameText;
            panelComp.levelText = levelText;
            panelComp.costText = costText;
            panelComp.upgradeButton = upgradeBtn;
            panelComp.upgradeButtonLabel = upgradeLabel;
            panelComp.closeButton = closeBtn;
            EditorUtility.SetDirty(panelComp);

            return panelComp;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize,
            Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static Button CreateButton(string name, Transform parent, string label,
            Vector2 pos, Vector2 size, Color color, out TMP_Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            labelText = CreateText(name + "Label", go.transform, label, 34, Vector2.zero, size, TextAlignmentOptions.Center);
            labelText.raycastTarget = false; // let the button receive the click
            return btn;
        }

        // ── Generic helpers ──────────────────────────────────────────────────────

        private static GameObject EnsureCanvas()
        {
            var go = GameObject.Find("EconomyCanvas");
            if (go == null)
            {
                go = new GameObject("EconomyCanvas");
                var c = go.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                var s = go.AddComponent<CanvasScaler>();
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1080, 1920);
                go.AddComponent<GraphicRaycaster>();
            }
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets", "Data");
        }

        private static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
