using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>One active status effect on a unit. Shields store their remaining pool in <see cref="magnitude"/>.</summary>
    public class ActiveEffect
    {
        public EffectType type;
        public float magnitude;     // percent for buffs, HP/sec for HoT, current pool for Shield
        public float remaining;     // seconds left; float.PositiveInfinity = permanent (talents)
    }

    /// <summary>
    /// A runtime combatant. Moves in its team's forward direction, stops to attack any
    /// opponent within range, and dies at 0 HP. Optionally carries a CharacterDefinition,
    /// which gives it a passive Talent and a charging Skill plus a status-effect system
    /// (buffs, shields, stun, slow, heal-over-time, taunt, multishot).
    ///
    /// Units created from a bare CombatUnitDefinition (enemies, the old test squad) have
    /// no abilities and behave exactly as before.
    /// </summary>
    public class CombatUnit : MonoBehaviour
    {
        public CombatUnitDefinition def;
        public CharacterDefinition character;   // player-side, null for enemies / plain units
        public EnemyDefinition enemy;           // enemy-side, null otherwise
        public CombatTeam team;

        // Enemy path/tower-defense data (used from Phase 3/5 on).
        [System.NonSerialized] public int blockCost = 1;
        [System.NonSerialized] public int leakDamage = 1;

        // Set for player units placed on a grid cell, so the cell frees on death.
        [System.NonSerialized] public Vector2Int cell;
        [System.NonSerialized] public bool occupiesCell;

        private float _hp;
        private float _attackTimer;
        private SpriteRenderer _body;
        private Transform _hpFill;
        private Transform _chargeFill;
        private float _baseY;

        // Abilities
        private Ability _talent, _skill;
        private bool _hasSkill;
        private float _charge;
        private float _auraTimer;
        private int _multiShotExtra;

        private readonly List<ActiveEffect> _effects = new List<ActiveEffect>();
        private static readonly List<CombatUnit> _scratch = new List<CombatUnit>();

        public bool IsEnemy => team == CombatTeam.Enemy;
        public bool IsAlive => _hp > 0f;
        public float HealthFraction => def != null && def.maxHP > 0f ? Mathf.Clamp01(_hp / def.maxHP) : 0f;
        public string DisplayName =>
            character != null ? character.displayName :
            enemy != null ? enemy.displayName :
            def != null ? def.displayName : "Unit";
        public string SkillName => _skill != null ? _skill.displayName : "";
        public bool HasManualSkill => _hasSkill && _skill.IsManual;
        public bool SkillReady => _hasSkill && _charge >= _skill.chargeTime;
        public float ChargeFraction => _hasSkill && _skill.chargeTime > 0f ? Mathf.Clamp01(_charge / _skill.chargeTime) : 0f;

        // ── Init ──────────────────────────────────────────────────────────────

        public void Init(CombatUnitDefinition definition, CombatTeam t, float baseY)
        {
            def = definition;
            character = null;
            SetupCommon(t, baseY);
        }

        public void InitCharacter(CharacterDefinition ch, CombatTeam t, float baseY)
        {
            character = ch;
            def = ch.combatStats;
            _talent = ch.HasTalent ? ch.talent : null;
            _skill = ch.HasSkill ? ch.skill : null;
            SetupCommon(t, baseY);
        }

        public void InitEnemy(EnemyDefinition e, CombatTeam t, float baseY)
        {
            enemy = e;
            def = e.combatStats;
            _talent = e.HasTalent ? e.talent : null;
            _skill = e.HasSkill ? e.skill : null;
            blockCost = Mathf.Max(1, e.blockCost);
            leakDamage = Mathf.Max(0, e.leakDamage);
            SetupCommon(t, baseY);
        }

        private void SetupCommon(CombatTeam t, float baseY)
        {
            team = t;
            _baseY = baseY;
            _hp = def.maxHP;
            _hasSkill = _skill != null && _skill.HasEffect && _skill.kind == AbilityKind.Skill;
            BuildVisual();
            ApplyTalentOnSpawn();
        }

        void OnEnable()  { BattleController.Register(this); }
        void OnDisable() { BattleController.Unregister(this); }

        // ── Main loop ───────────────────────────────────────────────────────────

        void Update()
        {
            if (BattleController.Instance == null || !BattleController.Instance.IsFighting) return;
            float dt = Time.deltaTime;

            TickEffects(dt);
            TickAura(dt);

            if (IsStunned()) return;   // stunned: cannot act or move

            ChargeSkill(dt);
            TryAutoFire();

            var target = BattleController.Instance.NearestOpponentWithin(this, def.attackRange);
            if (target != null)
            {
                _attackTimer -= dt;
                if (_attackTimer <= 0f)
                {
                    Attack(target);
                    _attackTimer = def.attackInterval / Mathf.Max(0.05f, AttackSpeedMult());
                    Flash();
                }
                return; // stand and fight
            }

            if (def.behavior == MoveBehavior.Advance)
            {
                float dir = IsEnemy ? -1f : 1f;
                transform.position += new Vector3(0f, dir * def.moveSpeed * MoveMult() * dt, 0f);
            }

            if (IsEnemy && transform.position.y <= _baseY)
            {
                BattleController.Instance.DamageBase(def.damage);
                Destroy(gameObject);
            }
        }

        void Attack(CombatUnit primary)
        {
            float dmg = def.damage * DamageMult();
            primary.TakeDamage(dmg);

            if (_multiShotExtra > 0)
            {
                _scratch.Clear();
                BattleController.Instance.OpponentsWithin(this, def.attackRange, _scratch);
                int hit = 0;
                for (int i = 0; i < _scratch.Count && hit < _multiShotExtra; i++)
                {
                    if (_scratch[i] == primary) continue;
                    _scratch[i].TakeDamage(dmg);
                    hit++;
                }
            }
        }

        // ── Skill charging / firing ───────────────────────────────────────────

        void ChargeSkill(float dt)
        {
            if (!_hasSkill || SkillReady) return;
            _charge += dt;
        }

        void TryAutoFire()
        {
            if (!_hasSkill || _skill.IsManual || !SkillReady) return;
            if (!AutoConditionMet()) return;
            FireSkill();
        }

        /// <summary>Player-driven activation. Returns true if the skill fired.</summary>
        public bool TryActivateManualSkill()
        {
            if (!HasManualSkill || !SkillReady) return false;
            FireSkill();
            return true;
        }

        bool AutoConditionMet()
        {
            switch (_skill.autoCondition)
            {
                case AutoFireCondition.EnemyInRange:
                    return BattleController.Instance.NearestOpponentWithin(this, def.attackRange) != null;
                case AutoFireCondition.AllyWounded:
                    return BattleController.Instance.AnyAllyWounded(this, AbilityRadius(_skill), 0.85f);
                default:
                    return true; // WhenCharged
            }
        }

        void FireSkill()
        {
            BattleController.Instance.ResolveAbility(this, _skill);
            _charge = 0f;
            Flash();
        }

        static float AbilityRadius(Ability a) => a.radius > 0f ? a.radius : 9999f;

        // ── Talents ─────────────────────────────────────────────────────────────

        void ApplyTalentOnSpawn()
        {
            if (_talent == null || !_talent.HasEffect) return;

            if (_talent.effect == EffectType.MultiShot)
            {
                _multiShotExtra = Mathf.Max(0, Mathf.RoundToInt(_talent.magnitude));
                return;
            }

            // Self-only talents are permanent buffs on this unit.
            if (_talent.target == EffectTarget.SelfOnly)
                AddEffect(_talent.effect, _talent.magnitude, float.PositiveInfinity);
            // Aura talents (allies in radius) are re-applied periodically in TickAura.
        }

        void TickAura(float dt)
        {
            if (_talent == null || _talent.target == EffectTarget.SelfOnly) return;
            if (_talent.effect == EffectType.None || _talent.effect == EffectType.MultiShot) return;

            _auraTimer -= dt;
            if (_auraTimer > 0f) return;
            _auraTimer = 0.5f;

            _scratch.Clear();
            BattleController.Instance.AlliesWithin(this, AbilityRadius(_talent), true, _scratch);
            for (int i = 0; i < _scratch.Count; i++)
                _scratch[i].AddEffect(_talent.effect, _talent.magnitude, 0.7f); // refreshes while in range
        }

        // ── Status effects ────────────────────────────────────────────────────

        /// <summary>Add an effect, or refresh the existing one of the same type (no stacking).</summary>
        public void AddEffect(EffectType type, float magnitude, float duration)
        {
            if (type == EffectType.None) return;
            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i].type != type) continue;
                _effects[i].magnitude = Mathf.Max(_effects[i].magnitude, magnitude);
                _effects[i].remaining = Mathf.Max(_effects[i].remaining, duration);
                return;
            }
            _effects.Add(new ActiveEffect { type = type, magnitude = magnitude, remaining = duration });
        }

        public void AddShield(float amount, float duration)
        {
            // Shields refresh to the larger pool / longer duration.
            AddEffect(EffectType.Shield, amount, duration <= 0f ? 6f : duration);
        }

        void TickEffects(float dt)
        {
            float hot = 0f;
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var e = _effects[i];
                if (e.type == EffectType.HealOverTime) hot += e.magnitude;
                if (!float.IsPositiveInfinity(e.remaining))
                {
                    e.remaining -= dt;
                    if (e.remaining <= 0f) { _effects.RemoveAt(i); continue; }
                }
            }
            if (hot > 0f) Heal(hot * dt);
        }

        bool IsStunned()
        {
            for (int i = 0; i < _effects.Count; i++)
                if (_effects[i].type == EffectType.Stun && _effects[i].remaining > 0f) return true;
            return false;
        }

        public bool IsTaunting()
        {
            for (int i = 0; i < _effects.Count; i++)
                if (_effects[i].type == EffectType.Taunt && _effects[i].remaining > 0f) return true;
            return false;
        }

        float SumPercent(EffectType type)
        {
            float s = 0f;
            for (int i = 0; i < _effects.Count; i++)
                if (_effects[i].type == type) s += _effects[i].magnitude;
            return s;
        }

        float DamageMult()      => 1f + SumPercent(EffectType.DamageBoost) / 100f;
        float AttackSpeedMult() => (1f + SumPercent(EffectType.AttackSpeedBoost) / 100f) * (1f - SlowFrac());
        float MoveMult()        => 1f - SlowFrac();
        float SlowFrac()        => Mathf.Clamp01(SumPercent(EffectType.Slow) / 100f);
        float DamageReductionFrac() => Mathf.Clamp01(SumPercent(EffectType.DamageReduction) / 100f);

        // ── Damage / healing ────────────────────────────────────────────────────

        public void TakeDamage(float amount) => ApplyDamage(amount, ignoreReduction: false);
        public void TakePureDamage(float amount) => ApplyDamage(amount, ignoreReduction: true);

        void ApplyDamage(float amount, bool ignoreReduction)
        {
            if (amount <= 0f) return;
            if (!ignoreReduction) amount *= (1f - DamageReductionFrac());

            // Shields absorb first.
            for (int i = _effects.Count - 1; i >= 0 && amount > 0f; i--)
            {
                var e = _effects[i];
                if (e.type != EffectType.Shield) continue;
                float absorbed = Mathf.Min(e.magnitude, amount);
                e.magnitude -= absorbed;
                amount -= absorbed;
                if (e.magnitude <= 0f) _effects.RemoveAt(i);
            }

            if (amount <= 0f) { UpdateHealthBar(); return; }

            _hp -= amount;
            UpdateHealthBar();
            if (_hp <= 0f) Destroy(gameObject);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || _hp <= 0f) return;
            _hp = Mathf.Min(def.maxHP, _hp + amount);
            UpdateHealthBar();
        }

        // ── Visuals ───────────────────────────────────────────────────────────

        void BuildVisual()
        {
            float d = def.bodyRadius * 2f;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(transform, false);
            bodyGO.transform.localScale = new Vector3(d, d, 1f);
            _body = bodyGO.AddComponent<SpriteRenderer>();
            _body.sprite = CircleSprite();
            _body.color = def.color;
            _body.sortingOrder = 10;

            float barY = def.bodyRadius + 0.18f;
            float barW = d;
            CreateBar("HPBarBg", new Color(0f, 0f, 0f, 0.6f), barW, barY, 11, out _);
            CreateBar("HPBarFill", new Color(0.3f, 0.9f, 0.35f, 1f), barW, barY, 12, out _hpFill);

            if (_hasSkill)
            {
                float chargeY = barY + 0.16f;
                CreateBar("ChargeBarBg", new Color(0f, 0f, 0f, 0.6f), barW, chargeY, 11, out _);
                CreateBar("ChargeBarFill", new Color(0.35f, 0.75f, 1f, 1f), barW, chargeY, 12, out _chargeFill);
                SetBarScale(_chargeFill, 0f);
            }
        }

        void CreateBar(string name, Color color, float width, float y, int order, out Transform t)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(width, 0.12f, 1f);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SquareSprite();
            sr.color = color;
            sr.sortingOrder = order;
            t = go.transform;
        }

        void UpdateHealthBar()
        {
            if (_hpFill == null) return;
            SetBarScale(_hpFill, Mathf.Clamp01(_hp / def.maxHP));
        }

        void LateUpdate()
        {
            if (_chargeFill == null) return;
            SetBarScale(_chargeFill, ChargeFraction);
            // ready-to-cast manual skills glow gold.
            var sr = _chargeFill.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = SkillReady ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0.35f, 0.75f, 1f, 1f);
        }

        void SetBarScale(Transform fill, float pct)
        {
            if (fill == null) return;
            var s = fill.localScale;
            fill.localScale = new Vector3((def.bodyRadius * 2f) * pct, s.y, 1f);
        }

        void Flash()
        {
            if (_body != null) _body.color = Color.Lerp(def.color, Color.white, 0.6f);
            Invoke(nameof(RestoreColor), 0.08f);
        }
        void RestoreColor() { if (_body != null) _body.color = def.color; }

        // ── Shared placeholder sprites ────────────────────────────────────────

        private static Sprite _circle;
        private static Sprite _square;

        static Sprite CircleSprite()
        {
            if (_circle != null) return _circle;
            const int s = 64;
            var tex = new Texture2D(s, s) { filterMode = FilterMode.Bilinear };
            var px = new Color[s * s];
            float r = s / 2f, cx = r, cy = r;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    px[y * s + x] = (dx * dx + dy * dy <= r * r) ? Color.white : Color.clear;
                }
            tex.SetPixels(px); tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _circle;
        }

        static Sprite SquareSprite()
        {
            if (_square != null) return _square;
            var tex = Texture2D.whiteTexture;
            _square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                    new Vector2(0.5f, 0.5f), tex.width);
            return _square;
        }
    }
}
