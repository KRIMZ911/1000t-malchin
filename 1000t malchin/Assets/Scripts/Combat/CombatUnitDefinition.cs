using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>Which side a combatant is on.</summary>
    public enum CombatTeam { Player, Enemy }

    /// <summary>Whether a unit advances toward the foe or holds its ground.</summary>
    public enum MoveBehavior { Hold, Advance }

    /// <summary>
    /// Stats for one kind of combatant (archer, horseman, enemy raider).
    /// Team is assigned when the unit is spawned, not here.
    /// Create via: Assets > Create > Malchin > Combat Unit Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Combat Unit Definition", fileName = "CombatUnitDefinition")]
    public class CombatUnitDefinition : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("Combat stats")]
        public float maxHP = 20f;
        public float damage = 5f;
        [Tooltip("Seconds between attacks.")]
        public float attackInterval = 1f;
        [Tooltip("Distance at which this unit can hit a foe (large = ranged).")]
        public float attackRange = 0.8f;
        [Tooltip("World units per second when advancing.")]
        public float moveSpeed = 1.5f;

        [Header("Behavior")]
        public MoveBehavior behavior = MoveBehavior.Advance;

        [Header("Placeholder look")]
        public Color color = Color.white;
        [Tooltip("Body radius in world units.")]
        public float bodyRadius = 0.35f;
    }
}
