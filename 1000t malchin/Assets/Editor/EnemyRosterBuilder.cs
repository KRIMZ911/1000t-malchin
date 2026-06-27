using UnityEditor;
using UnityEngine;
using Malchin.Combat;

namespace Malchin.EditorTools
{
    /// <summary>
    /// One-click generator for the enemy roster — the foe-side mirror of the player roster.
    /// Creates EnemyDefinitions (+ their combat stat blocks) under Assets/Data/Enemies. Some
    /// enemies carry abilities (a healer, an armored brute, a boss), reusing the same ability
    /// system the player characters use. Re-runnable: updates assets in place by file name.
    ///
    /// Menu:  Malchin > Create Enemy Roster
    /// </summary>
    public static class EnemyRosterBuilder
    {
        private const string Folder = "Assets/Data/Enemies";
        private const string StatFolder = "Assets/Data/Enemies/Stats";

        [MenuItem("Malchin/Create Enemy Roster")]
        public static void Build()
        {
            EnsureFolders();

            // Basic chaff — no abilities.
            Make("raider", "Steppe Raider", "Common marauder. Marches on the camp.",
                hp: 24, dmg: 5, interval: 1.0f, range: 0.9f, speed: 1.4f, MoveBehavior.Advance,
                new Color(0.90f, 0.30f, 0.30f), 0.38f, blockCost: 1, leak: 1, boss: false,
                talent: None(), skill: None());

            Make("wolfrider", "Wolf Rider", "Fast and fragile; tries to slip past the line.",
                hp: 16, dmg: 4, interval: 0.8f, range: 0.9f, speed: 2.6f, MoveBehavior.Advance,
                new Color(0.70f, 0.35f, 0.45f), 0.34f, blockCost: 1, leak: 1, boss: false,
                talent: None(), skill: None());

            // Armored brute — tanky, takes 2 block slots, has a damage-reduction talent.
            Make("brute", "Marauder Brute", "A hulking raider in scavenged armor; hard to stop.",
                hp: 90, dmg: 10, interval: 1.2f, range: 0.9f, speed: 1.0f, MoveBehavior.Advance,
                new Color(0.55f, 0.25f, 0.25f), 0.46f, blockCost: 2, leak: 1, boss: false,
                talent: Passive("Scrap Armor", "Takes 30% less damage.",
                    EffectType.DamageReduction, EffectTarget.SelfOnly, magnitude: 30f),
                skill: None());

            // Ranged harasser — attacks deployed units from a distance.
            Make("crossbow", "Crossbow Marauder", "Hangs back and shoots your deployed units.",
                hp: 22, dmg: 7, interval: 1.1f, range: 3.2f, speed: 1.2f, MoveBehavior.Advance,
                new Color(0.80f, 0.45f, 0.30f), 0.34f, blockCost: 1, leak: 1, boss: false,
                talent: None(), skill: None());

            // Enemy healer — keeps the horde alive (Auto: when an ally is hurt).
            Make("witch", "Witch of the Wastes", "A dark shaman that mends her fellow raiders.",
                hp: 30, dmg: 4, interval: 1.3f, range: 2.5f, speed: 1.0f, MoveBehavior.Advance,
                new Color(0.55f, 0.30f, 0.65f), 0.36f, blockCost: 1, leak: 1, boss: false,
                talent: None(),
                skill: Skill("Dark Mend", 7f, "Heals nearby raiders for 25. (Auto: when a raider is hurt.)",
                    EffectType.Heal, EffectTarget.AllAllies, magnitude: 25f, radius: 3f,
                    AoeShape.Circle, ShapeAnchor.Caster, shapeRadius: 3f,
                    condition: AutoFireCondition.AllyWounded));

            // Boss — high HP, takes 3 block slots, leaks hard, buffs allies, hits your units.
            Make("warlord", "Renegade Warlord", "The chapter boss. Rallies the horde and crushes defenders.",
                hp: 400, dmg: 22, interval: 1.0f, range: 1.1f, speed: 0.9f, MoveBehavior.Advance,
                new Color(0.85f, 0.20f, 0.20f), 0.55f, blockCost: 3, leak: 5, boss: true,
                talent: Passive("War Banner", "Nearby raiders deal +20% damage.",
                    EffectType.DamageBoost, EffectTarget.AlliesInRadius, magnitude: 20f, radius: 3.5f),
                skill: Skill("Crushing Sweep", 11f, "Smashes your nearby units for 35 damage.",
                    EffectType.AoeDamage, EffectTarget.EnemiesInRadius, magnitude: 35f, radius: 2f,
                    AoeShape.Circle, ShapeAnchor.TargetEnemy, shapeRadius: 2f,
                    condition: AutoFireCondition.EnemyInRange));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=lime>Malchin: enemy roster created</color> under " + Folder +
                      ". Assign these in a level's spawn timeline (Level inspector / Map Editor).");
        }

        // ── Builders ─────────────────────────────────────────────────────────────

        private static void Make(string id, string name, string desc,
            float hp, float dmg, float interval, float range, float speed, MoveBehavior behavior,
            Color color, float radius, int blockCost, int leak, bool boss, Ability talent, Ability skill)
        {
            var stats = CreateStats($"{id}_stats", id, name, hp, dmg, interval, range, speed, behavior, color, radius);

            string path = $"{Folder}/{Cap(id)}.asset";
            var e = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (e == null)
            {
                e = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(e, path);
            }
            e.id = id;
            e.displayName = name;
            e.description = desc;
            e.combatStats = stats;
            e.blockCost = blockCost;
            e.leakDamage = leak;
            e.isBoss = boss;
            e.talent = talent;
            e.skill = skill;
            EditorUtility.SetDirty(e);
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

        private static Ability Passive(string name, string desc, EffectType effect, EffectTarget target,
            float magnitude, float radius = 0f)
            => new Ability
            {
                displayName = name, description = desc, kind = AbilityKind.Talent,
                trigger = SkillTrigger.None, chargeTime = 0f,
                effect = effect, target = target, magnitude = magnitude, radius = radius
            };

        // Enemy skills are always Auto (no manual activation for the foe).
        private static Ability Skill(string name, float charge, string desc, EffectType effect,
            EffectTarget target, float magnitude, float radius,
            AoeShape shape, ShapeAnchor anchor, float shapeRadius,
            AutoFireCondition condition = AutoFireCondition.WhenCharged)
            => new Ability
            {
                displayName = name, description = desc, kind = AbilityKind.Skill,
                trigger = SkillTrigger.AutoCooldown, chargeTime = charge,
                activation = ActivationMode.Auto, autoCondition = condition,
                effect = effect, target = target, magnitude = magnitude, radius = radius,
                useCustomShape = true,
                area = new AreaShape { shape = shape, anchor = anchor, radius = shapeRadius }
            };

        private static Ability None() => new Ability { kind = AbilityKind.Skill, effect = EffectType.None };

        private static string Cap(string id) => char.ToUpper(id[0]) + id.Substring(1);

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Data", "Enemies");
            if (!AssetDatabase.IsValidFolder(StatFolder))
                AssetDatabase.CreateFolder(Folder, "Stats");
        }
    }
}
