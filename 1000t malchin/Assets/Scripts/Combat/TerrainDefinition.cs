using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>Tile height: melee deploy on Low, ranged deploy on High.</summary>
    public enum TerrainElevation { Low, High }

    /// <summary>What can be deployed on a tile.</summary>
    public enum TerrainDeploy { None, MeleeOnly, RangedOnly, Both }

    /// <summary>Who an on-tile effect applies to (units standing on the tile).</summary>
    public enum TileAffects { Enemies, Allies, Both }

    /// <summary>
    /// One reusable terrain type — a building block for maps (the "map pool").
    /// A map is a grid of these tiles. Each terrain governs deployment, whether enemies
    /// can walk it, how fast things cross it, and an OPTIONAL on-tile effect chosen from
    /// the same effect vocabulary the abilities use (so terrain recycles that system).
    ///
    /// Art: assign a small <see cref="tileSprite"/> per terrain later; until then the
    /// placeholder <see cref="color"/> is used. The whole-map background lives on the
    /// LevelDefinition, not here.
    ///
    /// Create via the generator (Malchin > Create Terrain Palette) or by hand:
    /// Assets > Create > Malchin > Terrain Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Terrain Definition", fileName = "Terrain")]
    public class TerrainDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Look (texture optional; color is the placeholder)")]
        public Sprite tileSprite;
        public Color color = Color.white;

        [Header("Rules")]
        public TerrainElevation elevation = TerrainElevation.Low;
        public TerrainDeploy deploy = TerrainDeploy.MeleeOnly;
        [Tooltip("Can enemies (and pathing) cross this tile? Off = impassable wall, shapes the path.")]
        public bool enemiesCanWalk = true;
        [Tooltip("Speed multiplier for anything crossing this tile. <1 slows, >1 speeds up.")]
        [Min(0f)] public float moveMultiplier = 1f;
        [Tooltip("Units on this tile can't be targeted at range until an enemy is adjacent (e.g. tall reeds).")]
        public bool concealsOccupant = false;

        [Header("On-tile effect (pick any; applied continuously while a unit stands here)")]
        [Tooltip("Reuses the ability effect vocabulary. None = no effect. " +
                 "Continuous reading: Heal/HealOverTime/AoeDamage = per second; Slow/buffs = while on tile.")]
        public EffectType onTileEffect = EffectType.None;
        public float effectMagnitude;
        public TileAffects effectAffects = TileAffects.Enemies;

        public bool HasEffect => onTileEffect != EffectType.None;
        public bool IsImpassable => !enemiesCanWalk && deploy == TerrainDeploy.None;
    }
}
