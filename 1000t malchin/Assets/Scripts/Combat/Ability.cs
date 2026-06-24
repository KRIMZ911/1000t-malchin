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
    /// One ability (a Talent or a Skill) attached to a CharacterDefinition.
    /// Plain serializable data — travels with its character, tunable in the Inspector.
    /// </summary>
    [System.Serializable]
    public class Ability
    {
        public string displayName;
        [TextArea] public string description;

        public AbilityKind kind = AbilityKind.Skill;

        [Header("Skill timing (ignored for Talents)")]
        public SkillTrigger trigger = SkillTrigger.AutoCooldown;
        [Tooltip("Seconds to charge before an AutoCooldown skill fires (and between casts).")]
        [Min(0f)] public float chargeTime = 10f;

        [Header("Effect")]
        public EffectType effect = EffectType.None;
        public EffectTarget target = EffectTarget.SelfOnly;
        [Tooltip("Meaning depends on effect: percent for buffs, flat HP/damage for heal/burst, count for MultiShot.")]
        public float magnitude;
        [Tooltip("Seconds the effect lasts (buffs, DoTs, stun, slow, shield). 0 = instant.")]
        [Min(0f)] public float duration;
        [Tooltip("World-unit radius for aura / AoE effects. 0 = single target / self.")]
        [Min(0f)] public float radius;

        public bool HasEffect => effect != EffectType.None;
    }
}
