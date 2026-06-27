using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click setup for the Phase 3 combat slice. Creates unit definitions,
    /// the battlefield (off in its own world region), the BattleController, and
    /// the battle HUD (entry button + deploy controls + result screen).
    ///
    /// Run AFTER the economy + building setups.
    /// Menu:  Malchin > Setup Combat Scene
    /// </summary>
    public static class CombatSceneSetup
    {
        private const string DataFolder = "Assets/Data";
        private static readonly Vector3 Anchor = new Vector3(100f, 0f, 0f);

        [MenuItem("Malchin/Setup Combat Scene")]
        public static void Setup()
        {
            EnsureFolder();

            // 1. Unit definitions -----------------------------------------------------
            var archer = CreateUnit("CombatArcher", "archer", "Archer",
                maxHP: 16, dmg: 4, interval: 0.6f, range: 3.5f, speed: 0f,
                MoveBehavior.Hold, new Color(0.30f, 0.55f, 0.95f), 0.30f);

            var horseman = CreateUnit("CombatHorseman", "horseman", "Horseman",
                maxHP: 45, dmg: 8, interval: 0.8f, range: 0.9f, speed: 2.2f,
                MoveBehavior.Advance, new Color(0.20f, 0.80f, 0.85f), 0.40f);

            // Enemy as a first-class enemy character. Prefer the generated enemy roster's
            // raider if present; otherwise build a simple inline one so setup is self-contained.
            var raider = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataFolder}/Enemies/Raider.asset");
            if (raider == null)
            {
                var raiderStats = CreateUnit("CombatRaider", "raider", "Steppe Raider",
                    maxHP: 24, dmg: 5, interval: 1.0f, range: 0.9f, speed: 1.4f,
                    MoveBehavior.Advance, new Color(0.90f, 0.30f, 0.30f), 0.38f);
                raider = CreateEnemy("EnemyRaider", "raider", "Steppe Raider", raiderStats, blockCost: 1, leak: 1);
            }

            // 2. Input plumbing -------------------------------------------------------
            EnsureEventSystem();
            EnsurePhysics2DRaycaster();

            // 3. Battlefield grid -----------------------------------------------------
            var grid = BuildBattleGrid();

            // 4. Sample level ---------------------------------------------------------
            var level = CreateSampleLevel(raider);

            // 5. BattleController -----------------------------------------------------
            var ctrl = GetOrAdd<BattleController>(FindOrCreate("BattleController"));
            ctrl.archerDef = archer;
            ctrl.horsemanDef = horseman;
            // If the starter roster exists, deploy collectible characters (with abilities)
            // as the test squad so skills/talents are visible. Run "Create Starter Roster" first.
            ctrl.archerCharacter   = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{DataFolder}/Characters/Khulan.asset");
            ctrl.horsemanCharacter = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{DataFolder}/Characters/Sukhbaatar.asset");
            ctrl.grid = grid;
            ctrl.level = level;
            ctrl.battleAnchor = Anchor;

            // 6. HUD ------------------------------------------------------------------
            var canvas = EnsureCanvas();
            ctrl.hud = BuildBattleHUD(canvas.transform, ctrl);
            EditorUtility.SetDirty(ctrl);

            // Save --------------------------------------------------------------------
            AssetDatabase.SaveAssets();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=lime>Malchin: combat scene setup complete.</color> " +
                      "Press Play, tap the Battle button, pick Archer/Horseman, then tap the lane to deploy.");
        }

        // ── Unit definitions ─────────────────────────────────────────────────────

        private static CombatUnitDefinition CreateUnit(string assetName, string id, string display,
            float maxHP, float dmg, float interval, float range, float speed,
            MoveBehavior behavior, Color color, float radius)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CombatUnitDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CombatUnitDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = display;
            def.maxHP = maxHP;
            def.damage = dmg;
            def.attackInterval = interval;
            def.attackRange = range;
            def.moveSpeed = speed;
            def.behavior = behavior;
            def.color = color;
            def.bodyRadius = radius;
            EditorUtility.SetDirty(def);
            return def;
        }

        private static EnemyDefinition CreateEnemy(string assetName, string id, string display,
            CombatUnitDefinition stats, int blockCost, int leak)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var e = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (e == null)
            {
                e = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(e, path);
            }
            e.id = id;
            e.displayName = display;
            e.combatStats = stats;
            e.blockCost = blockCost;
            e.leakDamage = leak;
            EditorUtility.SetDirty(e);
            return e;
        }

        // ── Battlefield + level ──────────────────────────────────────────────────

        private static BattleGrid BuildBattleGrid()
        {
            var go = FindOrCreate("BattleField");
            go.transform.position = Anchor;
            var grid = GetOrAdd<BattleGrid>(go);   // pulls in BoxCollider2D
            GetOrAdd<BattleFieldInput>(go);
            return grid;
        }

        private static LevelDefinition CreateSampleLevel(EnemyDefinition enemy)
        {
            string path = $"{DataFolder}/Level_01.asset";
            var lvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (lvl == null)
            {
                lvl = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(lvl, path);
            }
            lvl.levelName = "Level 01";
            lvl.gridWidth = 6;
            lvl.gridHeight = 8;
            lvl.cellSize = 1f;
            lvl.baseMaxHP = 10f;
            lvl.rewardSheep = 40;
            lvl.rewardCattle = 6;
            lvl.rewardHorse = 1;
            lvl.spawns = new List<EnemySpawn>
            {
                Spawn(enemy, 0.5f, 2), Spawn(enemy, 2f, 4),  Spawn(enemy, 3.5f, 1),
                Spawn(enemy, 5f, 3),   Spawn(enemy, 6.5f, 0), Spawn(enemy, 7f, 5),
                Spawn(enemy, 9f, 2),   Spawn(enemy, 10.5f, 4),
            };
            EditorUtility.SetDirty(lvl);
            return lvl;
        }

        private static EnemySpawn Spawn(EnemyDefinition enemy, float time, int column)
            => new EnemySpawn { enemy = enemy, time = time, column = column };

        [MenuItem("Malchin/Create Battle Level")]
        public static void CreateBattleLevel()
        {
            EnsureFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/Level_New.asset");
            var lvl = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(lvl, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = lvl;
            EditorGUIUtility.PingObject(lvl);
            Debug.Log($"Malchin: created new level at {path}. Edit it in the Inspector, then assign it to BattleController.");
        }

        // ── Battle HUD ───────────────────────────────────────────────────────────

        private static BattleHUD BuildBattleHUD(Transform canvas, BattleController ctrl)
        {
            var existing = canvas.Find("BattleHUD");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var host = new GameObject("BattleHUD", typeof(RectTransform));
            host.transform.SetParent(canvas, false);
            Stretch(host.GetComponent<RectTransform>());
            var hud = host.AddComponent<BattleHUD>();
            hud.controller = ctrl;

            // Base-view entry button (bottom center)
            var battleBtn = CreateButton("BattleButton", host.transform, "Battle",
                AnchorBottom, new Vector2(0f, 150f), new Vector2(400f, 130f),
                new Color(0.55f, 0.30f, 0.20f), out _);

            // In-battle panel
            var battlePanel = new GameObject("BattlePanel", typeof(RectTransform));
            battlePanel.transform.SetParent(host.transform, false);
            Stretch(battlePanel.GetComponent<RectTransform>());

            var baseHp = CreateText("BaseHP", battlePanel.transform, "Base HP: 10/10", 40,
                AnchorTop, new Vector2(0f, -120f), new Vector2(700f, 60f), TextAlignmentOptions.Center);
            var enemies = CreateText("Enemies", battlePanel.transform, "Enemies left: 8", 36,
                AnchorTop, new Vector2(0f, -185f), new Vector2(700f, 55f), TextAlignmentOptions.Center);

            var archerBtn = CreateButton("ArcherButton", battlePanel.transform, "Archer (4)",
                AnchorBottom, new Vector2(-220f, 150f), new Vector2(360f, 150f),
                new Color(0.30f, 0.45f, 0.80f), out var archerLabel);
            var horseBtn = CreateButton("HorsemanButton", battlePanel.transform, "Horseman (4)",
                AnchorBottom, new Vector2(220f, 150f), new Vector2(360f, 150f),
                new Color(0.20f, 0.65f, 0.70f), out var horseLabel);

            // Manual-skill button: appears above the deploy row when a unit is tapped.
            var skillBtn = CreateButton("SkillButton", battlePanel.transform, "Use Skill",
                AnchorBottom, new Vector2(0f, 320f), new Vector2(560f, 120f),
                new Color(0.78f, 0.55f, 0.18f), out var skillLabel);
            skillBtn.gameObject.SetActive(false);

            // Result panel (centered box)
            var resultPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(host.transform, false);
            var rrt = resultPanel.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.sizeDelta = new Vector2(740f, 640f);
            resultPanel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f, 0.94f);

            var resultText = CreateText("ResultText", resultPanel.transform, "Victory!", 46,
                AnchorMid, new Vector2(0f, 80f), new Vector2(660f, 380f), TextAlignmentOptions.Center);
            var returnBtn = CreateButton("ReturnButton", resultPanel.transform, "Return",
                AnchorMid, new Vector2(0f, -210f), new Vector2(320f, 110f),
                new Color(0.30f, 0.50f, 0.32f), out _);

            // Wire references
            hud.battleButton = battleBtn;
            hud.battlePanel = battlePanel;
            hud.baseHpText = baseHp;
            hud.enemiesText = enemies;
            hud.archerButton = archerBtn;
            hud.archerLabel = archerLabel;
            hud.horsemanButton = horseBtn;
            hud.horsemanLabel = horseLabel;
            hud.skillButton = skillBtn;
            hud.skillLabel = skillLabel;
            hud.resultPanel = resultPanel;
            hud.resultText = resultText;
            hud.returnButton = returnBtn;
            EditorUtility.SetDirty(hud);

            return hud;
        }

        // ── UI helpers (anchor presets) ──────────────────────────────────────────

        private static readonly Vector2 AnchorMid = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 AnchorTop = new Vector2(0.5f, 1f);
        private static readonly Vector2 AnchorBottom = new Vector2(0.5f, 0f);

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize,
            Vector2 anchor, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
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
            Vector2 anchor, Vector2 pos, Vector2 size, Color color, out TMP_Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            labelText = CreateText(name + "Label", go.transform, label, 34, AnchorMid, Vector2.zero, size, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
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

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }
            else GetOrAdd<InputSystemUIInputModule>(es.gameObject);
        }

        private static void EnsurePhysics2DRaycaster()
        {
            var cam = Camera.main;
            if (cam != null) GetOrAdd<Physics2DRaycaster>(cam.gameObject);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
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
