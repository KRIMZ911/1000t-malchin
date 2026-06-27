using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>
    /// An enemy combatant — the foe-side mirror of CharacterDefinition. Carries a combat
    /// stat block plus the same Talent/Skill ability system the player roster uses (so some
    /// enemies have abilities), and enemy-specific path/tower-defense fields. Enemies are
    /// NOT collectible, so there's no rarity / potential / deploy cost here.
    ///
    /// Authored via the generator (Malchin > Create Enemy Roster) or by hand:
    /// Assets > Create > Malchin > Enemy Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Enemy Definition", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Combat")]
        [Tooltip("The stat block this enemy fights with (HP, damage, range, behavior, look).")]
        public CombatUnitDefinition combatStats;

        [Header("Path / tower-defense")]
        [Tooltip("How many block slots it occupies when a melee unit blocks it (Phase 5).")]
        [Min(1)] public int blockCost = 1;
        [Tooltip("Life points lost if it reaches the goal/base (Phase 3 life points).")]
        [Min(0)] public int leakDamage = 1;
        public bool isBoss = false;

        [Header("Abilities (optional — some enemies have them)")]
        public Ability talent = new Ability { kind = AbilityKind.Talent };
        public Ability skill = new Ability { kind = AbilityKind.Skill };

        public bool HasTalent => talent != null && talent.HasEffect;
        public bool HasSkill  => skill  != null && skill.HasEffect;
    }
}
