using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>
    /// One per-milestone Potential bonus (dupes raise a character's Potential 1→6).
    /// Applied cumulatively as the player absorbs duplicate copies.
    /// </summary>
    [System.Serializable]
    public class PotentialBonus
    {
        [Tooltip("Potential level this bonus unlocks at (2–6).")]
        public int potentialLevel = 2;
        public float bonusHP;
        public float bonusATK;
        [Tooltip("Change to deploy cost (negative = cheaper).")]
        public int deployCostDelta;
    }

    /// <summary>
    /// A collectible gacha character (Stage 2). Identity + rarity + its combat stat
    /// block (a CombatUnitDefinition) + abilities (a Talent and a Skill) + the
    /// Potential table used when duplicates are absorbed.
    ///
    /// Authored in code via the one-click generator (Malchin > Create Starter Roster)
    /// or by hand: Assets > Create > Malchin > Character Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Character Definition", fileName = "Character")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public string title;            // e.g. "the Sunlit Khan"
        [TextArea] public string lore;

        [Header("Collection")]
        public Rarity rarity = Rarity.Star3;
        public CharacterRole role = CharacterRole.Horseman;

        [Header("Combat")]
        [Tooltip("The stat block this character fights with (HP, damage, range, behavior...).")]
        public CombatUnitDefinition combatStats;
        [Tooltip("Squad cost to deploy (Stage 3 squad selection). Higher = stronger.")]
        [Min(0)] public int deployCost = 8;

        [Header("Abilities")]
        public Ability talent = new Ability { kind = AbilityKind.Talent };
        public Ability skill = new Ability { kind = AbilityKind.Skill };

        [Header("Potential (duplicates → power)")]
        [Tooltip("Cumulative bonuses applied as Potential rises from 2 to 6.")]
        public PotentialBonus[] potentialTable = new PotentialBonus[0];

        public bool HasTalent => talent != null && talent.HasEffect;
        public bool HasSkill  => skill  != null && skill.HasEffect;
        public int Stars => (int)rarity;
    }
}
