using UnityEditor;
using UnityEngine;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click generator for the starter terrain palette (the "map pool"). Creates a set
    /// of reusable TerrainDefinition assets under Assets/Data/Terrain that maps are built
    /// from. Re-runnable: existing terrains are updated in place (matched by file name).
    ///
    /// Each terrain has a selectable on-tile effect (recycling the ability effect system);
    /// assign per-tile textures + the map background later in the Inspector.
    ///
    /// Menu:  Malchin > Create Terrain Palette
    /// </summary>
    public static class TerrainPaletteBuilder
    {
        private const string Folder = "Assets/Data/Terrain";

        [MenuItem("Malchin/Create Terrain Palette")]
        public static void Build()
        {
            EnsureFolder();

            Make("grass", "Steppe Grass", "Open steppe. The default ground — deploy melee here.",
                new Color(0.36f, 0.52f, 0.24f), TerrainElevation.Low, TerrainDeploy.MeleeOnly,
                walk: true, move: 1f, conceal: false, EffectType.None, 0f, TileAffects.Enemies);

            Make("hill", "Rocky Hill", "High ground. Deploy ranged units here; they can't be blocked.",
                new Color(0.55f, 0.47f, 0.36f), TerrainElevation.High, TerrainDeploy.RangedOnly,
                walk: false, move: 1f, conceal: false, EffectType.None, 0f, TileAffects.Allies);

            Make("river", "River / Ravine", "Impassable water/ravine. Shapes the enemy path; nothing deploys.",
                new Color(0.24f, 0.42f, 0.68f), TerrainElevation.Low, TerrainDeploy.None,
                walk: false, move: 1f, conceal: false, EffectType.None, 0f, TileAffects.Enemies);

            Make("mud", "Marsh / Mud", "Boggy ground. Enemies crossing it are slowed.",
                new Color(0.32f, 0.30f, 0.20f), TerrainElevation.Low, TerrainDeploy.MeleeOnly,
                walk: true, move: 0.5f, conceal: false, EffectType.Slow, 40f, TileAffects.Enemies);

            Make("reeds", "Tall Reeds", "Concealing cover. Units here can't be shot at range until enemies are close.",
                new Color(0.45f, 0.55f, 0.30f), TerrainElevation.Low, TerrainDeploy.MeleeOnly,
                walk: true, move: 0.85f, conceal: true, EffectType.None, 0f, TileAffects.Allies);

            Make("ovoo", "Ovoo Shrine", "Sacred cairn. Allies on or near it are blessed (regenerate HP).",
                new Color(0.85f, 0.78f, 0.45f), TerrainElevation.Low, TerrainDeploy.None,
                walk: true, move: 1f, conceal: false, EffectType.HealOverTime, 2f, TileAffects.Allies);

            Make("brazier", "Scorched Ground", "Burning hazard. Damages whatever stands on it each second.",
                new Color(0.62f, 0.28f, 0.20f), TerrainElevation.Low, TerrainDeploy.None,
                walk: true, move: 1f, conceal: false, EffectType.AoeDamage, 3f, TileAffects.Both);

            Make("sand", "Sand / Snow", "Loose drifts. Everything crossing is slowed by the drag.",
                new Color(0.80f, 0.76f, 0.62f), TerrainElevation.Low, TerrainDeploy.MeleeOnly,
                walk: true, move: 0.6f, conceal: false, EffectType.Slow, 25f, TileAffects.Both);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=lime>Malchin: terrain palette created</color> under " + Folder +
                      ". Assign tile textures in the Inspector; tune each terrain's effect freely.");
        }

        private static void Make(string id, string name, string desc, Color color,
            TerrainElevation elevation, TerrainDeploy deploy, bool walk, float move, bool conceal,
            EffectType effect, float magnitude, TileAffects affects)
        {
            string path = $"{Folder}/{Cap(id)}.asset";
            var t = AssetDatabase.LoadAssetAtPath<TerrainDefinition>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<TerrainDefinition>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.id = id;
            t.displayName = name;
            t.description = desc;
            t.color = color;
            t.elevation = elevation;
            t.deploy = deploy;
            t.enemiesCanWalk = walk;
            t.moveMultiplier = move;
            t.concealsOccupant = conceal;
            t.onTileEffect = effect;
            t.effectMagnitude = magnitude;
            t.effectAffects = affects;
            EditorUtility.SetDirty(t);
        }

        private static string Cap(string id) => char.ToUpper(id[0]) + id.Substring(1);

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Data", "Terrain");
        }
    }
}
