using UnityEditor;
using UnityEngine;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click generator for the Stage 2 starter roster. Creates 12 collectible
    /// CharacterDefinitions (across all 6 rarities) plus each character's combat
    /// stat block, abilities (a Talent + a Skill), and Potential table — all as
    /// assets under Assets/Data/Characters.
    ///
    /// Re-runnable: existing assets are updated in place (matched by file name).
    /// Menu:  Malchin > Create Starter Roster
    /// </summary>
    public static class RosterBuilder
    {
        private const string CharFolder = "Assets/Data/Characters";
        private const string StatFolder = "Assets/Data/Characters/Stats";

        [MenuItem("Malchin/Create Starter Roster")]
        public static void Build()
        {
            EnsureFolders();

            // ── 6★ Legendary ────────────────────────────────────────────────────
            Make("naranbaatar", "Naranbaatar", "the Sunlit Khan", Rarity.Star6, CharacterRole.Horseman,
                "A warlord said to ride at the head of the dawn. Where his banner flies, his riders fight harder.",
                hp: 90, dmg: 16, interval: 0.8f, range: 1.0f, speed: 2.2f, MoveBehavior.Advance,
                Gold(0.95f, 0.55f), 0.42f, cost: 18,
                talent: Passive("Khan's Banner",
                    "Allies near Naranbaatar deal +15% damage.",
                    EffectType.DamageBoost, EffectTarget.AlliesInRadius, magnitude: 15f, radius: 3f),
                skill: Skill("Sunlit Charge", 12f,
                    "Surges forward, smashing nearby enemies for 40 damage and stunning them for 1.5s.",
                    EffectType.AoeDamage, EffectTarget.EnemiesInRadius, magnitude: 40f, duration: 0f, radius: 1.8f));

            Make("sarangerel", "Sarangerel", "the Moonlight Shaman", Rarity.Star6, CharacterRole.Shaman,
                "She reads the Eternal Blue Sky and calls its mercy down onto the wounded.",
                hp: 40, dmg: 6, interval: 1.2f, range: 3.0f, speed: 0f, MoveBehavior.Hold,
                new Color(0.65f, 0.85f, 1f), 0.34f, cost: 16,
                talent: Passive("Eternal Blue Sky",
                    "Nearby allies recover 2 HP per second.",
                    EffectType.HealOverTime, EffectTarget.AlliesInRadius, magnitude: 2f, radius: 2.5f, duration: 0f),
                skill: Skill("Tengri's Blessing", 15f,
                    "Heals all nearby allies for 35 and shields them for 30 over 6s.",
                    EffectType.Heal, EffectTarget.AllAllies, magnitude: 35f, duration: 0f, radius: 3f));

            // ── 5★ Epic ─────────────────────────────────────────────────────────
            Make("khulan", "Khulan", "the Wind-Rider", Rarity.Star5, CharacterRole.HorseArcher,
                "The signature horse archer — looses arrows faster than the wind that carries them.",
                hp: 22, dmg: 7, interval: 0.5f, range: 4.0f, speed: 0f, MoveBehavior.Hold,
                new Color(0.30f, 0.60f, 0.95f), 0.30f, cost: 14,
                talent: Passive("Twin Quiver",
                    "Each shot fires an extra arrow at the same target.",
                    EffectType.MultiShot, EffectTarget.SingleEnemy, magnitude: 1f),
                skill: Skill("Arrow Storm", 10f,
                    "Rains arrows on an area, dealing 18 damage to all enemies caught in it.",
                    EffectType.AoeDamage, EffectTarget.EnemiesInRadius, magnitude: 18f, duration: 0f, radius: 2.5f));

            Make("ganbaatar", "Ganbaatar", "Ironwall", Rarity.Star5, CharacterRole.Defender,
                "An unmovable spearman. Lines break on him like water on stone.",
                hp: 120, dmg: 6, interval: 1.0f, range: 1.0f, speed: 0f, MoveBehavior.Hold,
                new Color(0.80f, 0.55f, 0.25f), 0.42f, cost: 15,
                talent: Passive("Stoneheart",
                    "Takes 25% less damage from all sources.",
                    EffectType.DamageReduction, EffectTarget.SelfOnly, magnitude: 25f),
                skill: Skill("Shieldwall", 14f,
                    "Raises a wall: shields nearby allies for 40 over 6s and taunts enemies to strike him.",
                    EffectType.Shield, EffectTarget.AlliesInRadius, magnitude: 40f, duration: 6f, radius: 2f));

            // ── 4★ Rare ─────────────────────────────────────────────────────────
            Make("tamir", "Tamir", "Swiftstream", Rarity.Star4, CharacterRole.HorseArcher,
                "A marksman who finds the gap in any armor.",
                hp: 20, dmg: 6, interval: 0.6f, range: 4.0f, speed: 0f, MoveBehavior.Hold,
                new Color(0.40f, 0.70f, 0.90f), 0.30f, cost: 11,
                talent: Passive("Eagle Eye",
                    "Attacks 15% faster.",
                    EffectType.AttackSpeedBoost, EffectTarget.SelfOnly, magnitude: 15f),
                skill: Skill("Piercing Shot", 8f,
                    "A heavy shot dealing 30 bonus damage that ignores armor.",
                    EffectType.ArmorPierce, EffectTarget.SingleEnemy, magnitude: 30f));

            Make("sukhbaatar", "Sukhbaatar", "the Axe", Rarity.Star4, CharacterRole.Horseman,
                "Swings a broad axe in wide, crushing arcs.",
                hp: 70, dmg: 12, interval: 0.9f, range: 1.0f, speed: 2.0f, MoveBehavior.Advance,
                new Color(0.25f, 0.75f, 0.80f), 0.40f, cost: 12,
                talent: Passive("Bloodlust",
                    "Deals 10% more damage.",
                    EffectType.DamageBoost, EffectTarget.SelfOnly, magnitude: 10f),
                skill: Skill("Cleave", 9f,
                    "A sweeping blow hitting all adjacent enemies for 25 damage.",
                    EffectType.AoeDamage, EffectTarget.EnemiesInRadius, magnitude: 25f, duration: 0f, radius: 1.5f));

            Make("oyun", "Oyun", "the Herbalist", Rarity.Star4, CharacterRole.Support,
                "Keeps a satchel of steppe remedies for the worst-hurt rider.",
                hp: 35, dmg: 4, interval: 1.3f, range: 2.5f, speed: 0f, MoveBehavior.Hold,
                new Color(0.50f, 0.85f, 0.55f), 0.32f, cost: 10,
                talent: Passive("Steady Hands",
                    "Slowly regenerates 1 HP per second.",
                    EffectType.HealOverTime, EffectTarget.SelfOnly, magnitude: 1f),
                skill: Skill("Healing Brew", 8f,
                    "Heals the most-wounded ally for 30.",
                    EffectType.Heal, EffectTarget.MostWoundedAlly, magnitude: 30f));

            // ── 3★ Common (talent only) ─────────────────────────────────────────
            Make("chuluun", "Chuluun", "Stoneguard", Rarity.Star3, CharacterRole.Defender,
                "Steady militia spearman, tough as the hills he grew up on.",
                hp: 80, dmg: 5, interval: 1.0f, range: 1.0f, speed: 0f, MoveBehavior.Hold,
                new Color(0.70f, 0.50f, 0.35f), 0.40f, cost: 9,
                talent: Passive("Tough Hide",
                    "Takes 12% less damage.",
                    EffectType.DamageReduction, EffectTarget.SelfOnly, magnitude: 12f),
                skill: None());

            Make("bataar", "Bataar", "Outrider", Rarity.Star3, CharacterRole.Horseman,
                "Eager young rider, quick to the charge.",
                hp: 50, dmg: 8, interval: 0.85f, range: 0.9f, speed: 2.2f, MoveBehavior.Advance,
                new Color(0.35f, 0.70f, 0.72f), 0.38f, cost: 8,
                talent: Passive("Vanguard",
                    "Attacks 8% faster.",
                    EffectType.AttackSpeedBoost, EffectTarget.SelfOnly, magnitude: 8f),
                skill: None());

            Make("naran", "Naran", "Scout", Rarity.Star3, CharacterRole.HorseArcher,
                "Sharp-eyed outrider who softens the enemy before they arrive.",
                hp: 18, dmg: 5, interval: 0.6f, range: 3.5f, speed: 0f, MoveBehavior.Hold,
                new Color(0.45f, 0.62f, 0.88f), 0.30f, cost: 8,
                talent: Passive("Marksman",
                    "Deals 8% more damage.",
                    EffectType.DamageBoost, EffectTarget.SelfOnly, magnitude: 8f),
                skill: None());

            // ── 2★ (plain stat unit) ────────────────────────────────────────────
            Make("erdene", "Erdene", "Levy Archer", Rarity.Star2, CharacterRole.HorseArcher,
                "A conscript bowman — numbers over skill.",
                hp: 14, dmg: 4, interval: 0.7f, range: 3.2f, speed: 0f, MoveBehavior.Hold,
                new Color(0.55f, 0.60f, 0.70f), 0.28f, cost: 6,
                talent: None(), skill: None());

            // ── 1★ (mascot homage) ──────────────────────────────────────────────
            Make("otgon", "Otgon", "the Herd-Boy", Rarity.Star1, CharacterRole.Horseman,
                "A herding child with a sling and more courage than sense. The face on the box.",
                hp: 22, dmg: 3, interval: 1.0f, range: 0.9f, speed: 1.8f, MoveBehavior.Advance,
                new Color(0.75f, 0.72f, 0.62f), 0.30f, cost: 4,
                talent: None(), skill: None());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=lime>Malchin: starter roster created (12 characters)</color> under " +
                      CharFolder + ". Inspect them, tune stats/abilities, then feed into the gacha pool.");
        }

        // ── Builders ─────────────────────────────────────────────────────────────

        private static void Make(string id, string name, string title, Rarity rarity, CharacterRole role,
            string lore, float hp, float dmg, float interval, float range, float speed, MoveBehavior behavior,
            Color color, float radius, int cost, Ability talent, Ability skill)
        {
            var stats = CreateStats($"{id}_stats", id, name, hp, dmg, interval, range, speed, behavior, color, radius);

            string path = $"{CharFolder}/{Cap(id)}.asset";
            var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(c, path);
            }
            c.id = id;
            c.displayName = name;
            c.title = title;
            c.lore = lore;
            c.rarity = rarity;
            c.role = role;
            c.combatStats = stats;
            c.deployCost = cost;
            c.talent = talent;
            c.skill = skill;
            c.potentialTable = StandardPotential(hp, dmg);
            EditorUtility.SetDirty(c);
        }

        private static CombatUnitDefinition CreateStats(string assetName, string id, string display,
            float hp, float dmg, float interval, float range, float speed, MoveBehavior behavior,
            Color color, float radius)
        {
            string path = $"{StatFolder}/{assetName}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CombatUnitDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CombatUnitDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.id = id;
            def.displayName = display;
            def.maxHP = hp;
            def.damage = dmg;
            def.attackInterval = interval;
            def.attackRange = range;
            def.moveSpeed = speed;
            def.behavior = behavior;
            def.color = color;
            def.bodyRadius = radius;
            EditorUtility.SetDirty(def);
            return def;
        }

        /// <summary>Pot 2 +HP, Pot 3 −1 cost, Pot 4 +ATK, Pot 5 −1 cost, Pot 6 +HP & +ATK.</summary>
        private static PotentialBonus[] StandardPotential(float baseHP, float baseATK)
        {
            float hpStep = Mathf.Round(baseHP * 0.08f);
            float atkStep = Mathf.Max(1f, Mathf.Round(baseATK * 0.08f));
            return new[]
            {
                new PotentialBonus { potentialLevel = 2, bonusHP = hpStep },
                new PotentialBonus { potentialLevel = 3, deployCostDelta = -1 },
                new PotentialBonus { potentialLevel = 4, bonusATK = atkStep },
                new PotentialBonus { potentialLevel = 5, deployCostDelta = -1 },
                new PotentialBonus { potentialLevel = 6, bonusHP = hpStep, bonusATK = atkStep },
            };
        }

        private static Ability Passive(string name, string desc, EffectType effect, EffectTarget target,
            float magnitude, float radius = 0f, float duration = 0f)
            => new Ability
            {
                displayName = name, description = desc, kind = AbilityKind.Talent,
                trigger = SkillTrigger.None, chargeTime = 0f,
                effect = effect, target = target, magnitude = magnitude, radius = radius, duration = duration
            };

        private static Ability Skill(string name, float charge, string desc, EffectType effect,
            EffectTarget target, float magnitude, float duration = 0f, float radius = 0f)
            => new Ability
            {
                displayName = name, description = desc, kind = AbilityKind.Skill,
                trigger = SkillTrigger.AutoCooldown, chargeTime = charge,
                effect = effect, target = target, magnitude = magnitude, duration = duration, radius = radius
            };

        private static Ability None() => new Ability { kind = AbilityKind.Skill, effect = EffectType.None };

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string Cap(string id) => char.ToUpper(id[0]) + id.Substring(1);

        private static Color Gold(float r, float g) => new Color(r, g, 0.20f);

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(CharFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Characters");
            if (!AssetDatabase.IsValidFolder(StatFolder))
                AssetDatabase.CreateFolder(CharFolder, "Stats");
        }
    }
}
