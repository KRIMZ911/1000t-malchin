using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>Collectible rarity, 1★–6★ (see vault `09 - Combat & Gacha Design`).</summary>
    public enum Rarity { Star1 = 1, Star2 = 2, Star3 = 3, Star4 = 4, Star5 = 5, Star6 = 6 }

    /// <summary>Battlefield archetype. Drives default behavior + how the unit is used.</summary>
    public enum CharacterRole { HorseArcher, Horseman, Defender, Support, Shaman }

    /// <summary>
    /// What an ability is:
    ///  - Talent: a passive, always-on effect (no charge).
    ///  - Skill:  charges, then resolves its effect (auto-casts in the auto-battler;
    ///            manual activation can be layered on later).
    /// </summary>
    public enum AbilityKind { Talent, Skill }

    /// <summary>When a Skill resolves. (Talents are always passive.)</summary>
    public enum SkillTrigger { None, AutoCooldown, OnAttack, OnDeploy }

    /// <summary>
    /// How a charged Skill is fired:
    ///  - Auto: the unit fires it itself when its AutoFireCondition is met.
    ///  - Manual: the player taps the unit and presses the skill button.
    /// </summary>
    public enum ActivationMode { Auto, Manual }

    /// <summary>For Auto skills: what must be true (in addition to being charged) to fire.</summary>
    public enum AutoFireCondition
    {
        WhenCharged,    // fire the instant the bar fills
        EnemyInRange,   // fire when charged AND a foe is within attack range ("enemy in front")
        AllyWounded     // fire when charged AND an ally (or self) is hurt
    }

    /// <summary>
    /// The vocabulary of effects a character can apply. Each Ability picks one.
    /// magnitude/duration/radius on the Ability give it meaning (see tooltips there).
    /// </summary>
    public enum EffectType
    {
        None,
        DamageBoost,        // +ATK%  (magnitude = percent)
        AttackSpeedBoost,   // faster attacks (magnitude = percent)
        Heal,               // instant HP (magnitude = flat HP)
        HealOverTime,       // HP per second for duration (magnitude = HP/sec)
        Shield,             // temporary absorb HP for duration (magnitude = flat HP)
        DamageReduction,    // incoming damage cut (magnitude = percent)
        AoeDamage,          // burst damage in radius (magnitude = flat damage)
        Slow,               // enemy move/attack speed down (magnitude = percent, duration secs)
        Stun,               // enemy cannot act (duration secs)
        ArmorPierce,        // bonus damage that ignores DamageReduction (magnitude = flat)
        Taunt,              // forces nearby enemies to target the caster (duration secs)
        MultiShot           // extra projectiles per attack (magnitude = extra count)
    }

    /// <summary>Who an effect lands on.</summary>
    public enum EffectTarget
    {
        SelfOnly,
        AllAllies,          // all allies in radius
        AlliesInRadius,
        MostWoundedAlly,
        SingleEnemy,
        EnemiesInRadius
    }

    /// <summary>
    /// The geometric form of an area, resolved in continuous world space (NOT grid cells)
    /// so it works against smoothly-moving enemies. The grid is only for placement.
    /// </summary>
    public enum AoeShape { Circle, Cone, Line }

    /// <summary>Where an area shape originates / is centered.</summary>
    public enum ShapeAnchor { Caster, TargetEnemy }

    /// <summary>Which way a cone or line points.</summary>
    public enum ShapeDirection { Forward, TowardNearestEnemy }

    /// <summary>
    /// A world-space area: a circle (burst), a cone (fan), or a line (beam/lane strip).
    /// <list type="bullet">
    /// <item><b>Circle</b> — within <see cref="radius"/> of the origin.</item>
    /// <item><b>Cone</b> — within <see cref="radius"/> length and <see cref="coneAngle"/> total angle of the direction.</item>
    /// <item><b>Line</b> — within <see cref="radius"/> length and <see cref="lineWidth"/> total width along the direction.</item>
    /// </list>
    /// "Forward" means toward the foe (player units face up the grid, enemies face down).
    /// </summary>
    [System.Serializable]
    public class AreaShape
    {
        public AoeShape shape = AoeShape.Circle;
        public ShapeAnchor anchor = ShapeAnchor.TargetEnemy;
        [Tooltip("Circle radius, or cone/line length, in world units.")]
        [Min(0f)] public float radius = 1.5f;
        [Tooltip("Cone only: total opening angle in degrees.")]
        [Min(0f)] public float coneAngle = 60f;
        [Tooltip("Line only: total width in world units.")]
        [Min(0f)] public float lineWidth = 1f;
        [Tooltip("Cone/Line only: which way the shape points.")]
        public ShapeDirection direction = ShapeDirection.Forward;
    }

    /// <summary>
    /// One ability (a Talent or a Skill) attached to a CharacterDefinition.
    /// Plain serializable data — travels with its character, tunable in the Inspector.
    /// </summary>
    [System.Serializable]
    public class Ability
    {
        public string displayName;
        [TextArea] public string description;

        public AbilityKind kind = AbilityKind.Skill;

        [Header("Skill timing + activation (ignored for Talents)")]
        public SkillTrigger trigger = SkillTrigger.AutoCooldown;
        [Tooltip("Seconds to charge before the skill can fire (and between casts).")]
        [Min(0f)] public float chargeTime = 10f;
        public ActivationMode activation = ActivationMode.Auto;
        [Tooltip("For Auto skills only: the extra condition to fire once charged.")]
        public AutoFireCondition autoCondition = AutoFireCondition.WhenCharged;

        [Header("Effect")]
        public EffectType effect = EffectType.None;
        public EffectTarget target = EffectTarget.SelfOnly;
        [Tooltip("Meaning depends on effect: percent for buffs, flat HP/damage for heal/burst, count for MultiShot.")]
        public float magnitude;
        [Tooltip("Seconds the effect lasts (buffs, DoTs, stun, slow, shield). 0 = instant.")]
        [Min(0f)] public float duration;
        [Tooltip("World-unit radius for aura / AoE effects. 0 = single target / self.")]
        [Min(0f)] public float radius;

        [Header("Secondary effect (optional, same targets)")]
        [Tooltip("A second effect applied alongside the primary, e.g. AoE damage + Stun.")]
        public EffectType secondaryEffect = EffectType.None;
        public float secondaryMagnitude;
        [Min(0f)] public float secondaryDuration;

        [Header("Area shape (for AlliesInRadius / EnemiesInRadius targets)")]
        [Tooltip("If on, use the AreaShape below. If off, falls back to a circle of 'radius' (legacy).")]
        public bool useCustomShape = false;
        public AreaShape area = new AreaShape();

        public bool HasEffect => effect != EffectType.None;
        public bool HasSecondary => secondaryEffect != EffectType.None;
        public bool IsManual => kind == AbilityKind.Skill && activation == ActivationMode.Manual;
    }
}
