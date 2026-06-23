using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Malchin.Economy;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click setup for the Phase 1 economy slice. Creates the livestock data
    /// assets, the HerdManager + Bootstrap objects, and the herd UI — then saves
    /// the scene. Re-running is safe: it reuses anything that already exists.
    ///
    /// Run from the Unity menu:  Malchin > Setup Economy Scene
    /// </summary>
    public static class EconomySceneSetup
    {
        private const string DataFolder   = "Assets/Data";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string RowPrefabPath = PrefabFolder + "/HerdRow.prefab";

        [MenuItem("Malchin/Setup Economy Scene")]
        public static void Setup()
        {
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);

            // 1. Livestock data assets ------------------------------------------------
            var sheep  = CreateLivestock("Sheep",        "sheep",         "Sheep",         10, 100, 6f,   Color.white);
            var cattle = CreateLivestock("Cattle",       "cattle",        "Cattle",         3,  30, 2f,   new Color(0.55f, 0.27f, 0.07f));
            var horse  = CreateLivestock("SpecialHorse", "special_horse", "Special Horse",  0,  10, 0.5f, new Color(1f, 0.84f, 0f));

            // 2. HerdManager ----------------------------------------------------------
            var herdGO = FindOrCreate("HerdManager");
            var herd = GetOrAdd<HerdManager>(herdGO);
            herd.livestockTypes = new System.Collections.Generic.List<LivestockDefinition> { sheep, cattle, horse };
            EditorUtility.SetDirty(herd);

            // 3. Bootstrap ------------------------------------------------------------
            GetOrAdd<GameBootstrap>(FindOrCreate("Bootstrap"));

            // 4. Canvas ---------------------------------------------------------------
            var canvasGO = FindOrCreate("EconomyCanvas");
            var canvas = GetOrAdd<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetOrAdd<CanvasScaler>(canvasGO);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            GetOrAdd<GraphicRaycaster>(canvasGO);

            // 5. Row container (top-left, vertical list) ------------------------------
            var container = FindChildOrCreate(canvasGO.transform, "RowContainer");
            var crt = GetOrAdd<RectTransform>(container);
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0f, 1f); // top-left
            crt.anchoredPosition = new Vector2(30f, -30f);
            crt.sizeDelta = new Vector2(500f, 0f);
            var vlg = GetOrAdd<VerticalLayoutGroup>(container);
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = GetOrAdd<ContentSizeFitter>(container);
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 6. Row prefab -----------------------------------------------------------
            var rowPrefab = BuildRowPrefab();

            // 7. HerdUI ---------------------------------------------------------------
            var herdUI = GetOrAdd<HerdUI>(canvasGO);
            herdUI.rowPrefab = rowPrefab;
            herdUI.rowContainer = container.transform;
            EditorUtility.SetDirty(herdUI);

            // Save everything ---------------------------------------------------------
            AssetDatabase.SaveAssets();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=lime>Malchin: economy scene setup complete.</color> Press Play to see the herd grow.");
        }

        // ── Row prefab construction ──────────────────────────────────────────────

        private static GameObject BuildRowPrefab()
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null)
                Debug.LogWarning("Malchin: TMP default font missing. Run Window > TextMeshPro > Import TMP Essential Resources, then re-run setup.");

            // Build a temporary instance, then save it as a prefab.
            var row = new GameObject("HerdRow", typeof(RectTransform));
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.minHeight = 50f;

            // Icon
            var icon = new GameObject("Icon", typeof(RectTransform));
            icon.transform.SetParent(row.transform, false);
            var iconImg = icon.AddComponent<Image>();
            var iconLE = icon.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 44f;
            iconLE.preferredHeight = 44f;

            // Label
            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(row.transform, false);
            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Name";
            labelTMP.fontSize = 32f;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) labelTMP.font = font;
            var labelLE = label.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 200f;

            // Count
            var count = new GameObject("CountText", typeof(RectTransform));
            count.transform.SetParent(row.transform, false);
            var countTMP = count.AddComponent<TextMeshProUGUI>();
            countTMP.text = "0 / 0";
            countTMP.fontSize = 32f;
            countTMP.alignment = TextAlignmentOptions.MidlineRight;
            if (font != null) countTMP.font = font;
            var countLE = count.AddComponent<LayoutElement>();
            countLE.flexibleWidth = 1f;

            // Wire the HerdRowUI references
            var rowUI = row.AddComponent<HerdRowUI>();
            rowUI.icon = iconImg;
            rowUI.label = labelTMP;
            rowUI.countText = countTMP;

            var prefab = PrefabUtility.SaveAsPrefabAsset(row, RowPrefabPath);
            Object.DestroyImmediate(row);
            return prefab;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static LivestockDefinition CreateLivestock(
            string assetName, string id, string display, int start, int cap, float perMin, Color color)
        {
            string path = $"{DataFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<LivestockDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<LivestockDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = display;
            def.startingCount = start;
            def.baseCap = cap;
            def.baseGrowthPerMinute = perMin;
            def.placeholderColor = color;
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        private static GameObject FindChildOrCreate(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
