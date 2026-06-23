using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>
    /// A runtime combatant. Moves in its team's forward direction (player = up,
    /// enemy = down toward the base), stops to attack any opponent within range,
    /// and dies at 0 HP. Enemies that reach the base damage it and despawn.
    /// Targeting uses simple distance checks via BattleController's unit list.
    /// </summary>
    public class CombatUnit : MonoBehaviour
    {
        public CombatUnitDefinition def;
        public CombatTeam team;

        // Set for player units placed on a grid cell, so the cell frees on death.
        [System.NonSerialized] public Vector2Int cell;
        [System.NonSerialized] public bool occupiesCell;

        private float _hp;
        private float _attackTimer;
        private SpriteRenderer _body;
        private Transform _hpFill;
        private float _baseY;

        public bool IsEnemy => team == CombatTeam.Enemy;

        public void Init(CombatUnitDefinition definition, CombatTeam t, float baseY)
        {
            def = definition;
            team = t;
            _baseY = baseY;
            _hp = def.maxHP;
            BuildVisual();
        }

        void OnEnable()  { BattleController.Register(this); }
        void OnDisable() { BattleController.Unregister(this); }

        void Update()
        {
            if (BattleController.Instance == null || !BattleController.Instance.IsFighting) return;

            var target = BattleController.Instance.NearestOpponentWithin(this, def.attackRange);
            if (target != null)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f)
                {
                    target.TakeDamage(def.damage);
                    _attackTimer = def.attackInterval;
                    Flash();
                }
                return; // stand and fight
            }

            if (def.behavior == MoveBehavior.Advance)
            {
                float dir = IsEnemy ? -1f : 1f;
                transform.position += new Vector3(0f, dir * def.moveSpeed * Time.deltaTime, 0f);
            }

            // An enemy that breaks through to the base damages it and is consumed.
            if (IsEnemy && transform.position.y <= _baseY)
            {
                BattleController.Instance.DamageBase(def.damage);
                Destroy(gameObject);
            }
        }

        public void TakeDamage(float amount)
        {
            _hp -= amount;
            UpdateHealthBar();
            if (_hp <= 0f) Destroy(gameObject);
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
            float pct = Mathf.Clamp01(_hp / def.maxHP);
            var s = _hpFill.localScale;
            _hpFill.localScale = new Vector3((def.bodyRadius * 2f) * pct, s.y, 1f);
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
