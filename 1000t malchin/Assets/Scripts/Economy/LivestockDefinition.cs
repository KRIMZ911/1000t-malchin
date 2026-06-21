using UnityEngine;
using UnityEngine.Serialization;

namespace Malchin.Economy
{
    /// <summary>
    /// Defines a single livestock type (sheep, cattle, special horse, etc.).
    /// Create assets via: Assets > Create > Malchin > Livestock Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Livestock Definition", fileName = "LivestockDefinition")]
    public class LivestockDefinition : ScriptableObject
    {
        [Tooltip("Internal key used in save data — never change after first save.")]
        public string id;

        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [Tooltip("Starting count when a new game begins.")]
        public int startingCount;

        [Tooltip("Maximum herd size (can be raised by Herding Ger upgrades).")]
        public int baseCap;

        [Tooltip("How many are added per real-time MINUTE (before Herding Ger bonuses). " +
                 "Drives both in-session and offline growth.")]
        [FormerlySerializedAs("baseGrowthPerTick")]
        public float baseGrowthPerMinute;

        [Tooltip("Color tint for placeholder UI icons.")]
        public Color placeholderColor = Color.white;
    }
}
