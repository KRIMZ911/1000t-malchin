using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Building
{
    /// <summary>How a ger's level affects the economy.</summary>
    public enum GerEffectType
    {
        None,             // decorative / future system (e.g. the Ovoo's summoning)
        GrowthMultiplier, // multiplies a livestock type's growth rate
        CapMultiplier     // multiplies a livestock type's maximum cap
    }

    [System.Serializable]
    public class LivestockCost
    {
        public string livestockId;
        public int amount;
    }

    [System.Serializable]
    public class GerLevel
    {
        [Tooltip("What it costs to upgrade INTO this level. Leave empty for the starting level (index 0).")]
        public List<LivestockCost> costToReach = new List<LivestockCost>();

        [Tooltip("Effect magnitude at this level, read according to the definition's effectType. " +
                 "e.g. 1.5 on a GrowthMultiplier ger = +50% growth.")]
        public float effectValue = 1f;

        [TextArea] public string note;
    }

    /// <summary>
    /// Defines one kind of ger (building): its identity, placeholder look, the
    /// economy effect it has, and its multi-level upgrade path.
    /// Create via: Assets > Create > Malchin > Ger Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Ger Definition", fileName = "GerDefinition")]
    public class GerDefinition : ScriptableObject
    {
        [Tooltip("Internal key — never change after first save.")]
        public string id;

        [Tooltip("Display name shown on the building and upgrade panel.")]
        public string displayName;

        [Tooltip("Color tint for the placeholder square.")]
        public Color placeholderColor = Color.gray;

        [Header("Grid footprint (in cells)")]
        [Min(1)] public int footprintWidth = 1;
        [Min(1)] public int footprintHeight = 1;

        [Header("Economy effect")]
        public GerEffectType effectType = GerEffectType.None;

        [Tooltip("Which livestock id the Growth/Cap multiplier applies to.")]
        public string effectTargetLivestockId;

        [Header("Upgrade path (element 0 = starting level)")]
        public List<GerLevel> levels = new List<GerLevel>();

        /// <summary>Highest valid level index (0-based).</summary>
        public int MaxLevelIndex => Mathf.Max(0, levels.Count - 1);

        public bool CanUpgradeFrom(int level) => level < MaxLevelIndex;

        public GerLevel GetLevel(int index)
        {
            if (levels.Count == 0) return new GerLevel();
            return levels[Mathf.Clamp(index, 0, MaxLevelIndex)];
        }

        /// <summary>The cost to upgrade from <paramref name="currentLevel"/> to the next level, or null if maxed.</summary>
        public List<LivestockCost> CostToUpgradeFrom(int currentLevel)
        {
            if (!CanUpgradeFrom(currentLevel)) return null;
            return levels[currentLevel + 1].costToReach;
        }
    }
}
