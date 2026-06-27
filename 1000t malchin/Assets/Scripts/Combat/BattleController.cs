using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Malchin.Economy;

namespace Malchin.Combat
{
    public enum BattleState { Idle, Fighting, Won, Lost }

    /// <summary>
    /// Runs a grid battle defined by a LevelDefinition. Builds the grid, plays the
    /// spawn timeline, lets the player deploy their squad onto cells, resolves
    /// win/lose, and rewards livestock. Lives in its own world region; the camera
    /// pans there on StartBattle and back on Return.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public static BattleController Instance { get; private set; }
        private static readonly List<CombatUnit> _units = new List<CombatUnit>();

        [Header("Level + grid")]
        public LevelDefinition level;
        public BattleGrid grid;
        public Vector3 battleAnchor = new Vector3(100f, 0f, 0f);

        [Header("Player test squad (Stage 1)")]
        public CombatUnitDefinition archerDef;
        public CombatUnitDefinition horsemanDef;
        [Tooltip("Optional: deploy these collectible characters (with abilities) instead of the bare defs.")]
        public CharacterDefinition archerCharacter;
        public CharacterDefinition horsemanCharacter;
        public int archerCount = 4;
        public int horsemanCount = 4;

        public BattleHUD hud;

        public BattleState State { get; private set; } = BattleState.Idle;
        public bool IsFighting => State == BattleState.Fighting;

        private float _baseHP;
        private float _elapsed;
        private int _spawnIndex;
        private int _enemiesAlive;
        private List<EnemySpawn> _schedule;
        private int _archersLeft, _horsemenLeft;
        private enum DeploySlot { Archer, Horseman }
        private DeploySlot _selectedSlot = DeploySlot.Archer;
        private CombatUnit _selectedUnit;   // a deployed unit tapped for manual-skill use
        private readonly Dictionary<Vector2Int, CombatUnit> _playerCells = new Dictionary<Vector2Int, CombatUnit>();

        private Camera _cam;
        private Vector3 _savedCamPos;
        private float _savedCamSize;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cam = Camera.main;
        }

        // ── Unit registry ─────────────────────────────────────────────────────

        public static void Register(CombatUnit u) { if (!_units.Contains(u)) _units.Add(u); }

        public static void Unregister(CombatUnit u)
        {
            _units.Remove(u);
            if (Instance != null) Instance.OnUnitRemoved(u);
        }

        void OnUnitRemoved(CombatUnit u)
        {
            if (u.occupiesCell) _playerCells.Remove(u.cell);
            if (_selectedUnit == u) { _selectedUnit = null; if (hud != null) hud.HideSkillButton(); }
            if (State == BattleState.Fighting && u.IsEnemy)
            {
                _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
                CheckWin();
            }
            UpdateHud();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public void StartBattle()
        {
            if (State == BattleState.Fighting) return;
            if (level == null) { Debug.LogWarning("BattleController: no LevelDefinition assigned."); return; }
            if (grid == null) { Debug.LogWarning("BattleController: no BattleGrid assigned."); return; }

            ClearUnits();
            grid.transform.position = battleAnchor;
            grid.Configure(level);   // renders terrain tiles + background

            State = BattleState.Fighting;
            _baseHP = level.baseMaxHP;
            _elapsed = 0f;
            _spawnIndex = 0;
            _enemiesAlive = 0;
            _schedule = level.SortedSpawns();
            _archersLeft = archerCount;
            _horsemenLeft = horsemanCount;
            _selectedSlot = DeploySlot.Archer;
            _selectedUnit = null;
            _playerCells.Clear();

            FrameCamera();
            if (hud != null) hud.OnBattleStarted();
            UpdateHud();
        }

        void Update()
        {
            if (State != BattleState.Fighting) return;

            _elapsed += Time.deltaTime;
            while (_schedule != null && _spawnIndex < _schedule.Count && _schedule[_spawnIndex].time <= _elapsed)
            {
                SpawnFromEntry(_schedule[_spawnIndex]);
                _spawnIndex++;
            }

            // Keep the manual-skill button's enabled state in sync with the unit's charge.
            if (_selectedUnit != null && hud != null)
                hud.SetSkillButtonReady(_selectedUnit.SkillReady);
        }

        void SpawnFromEntry(EnemySpawn entry)
        {
            if (entry.enemy == null || entry.enemy.combatStats == null) return;
            _enemiesAlive++;
            int col = Mathf.Clamp(entry.column, 0, level.gridWidth - 1);
            var pos = new Vector3(grid.ColumnX(col), grid.TopY + 0.4f, 0f);
            SpawnEnemy(entry.enemy, pos);
            UpdateHud();
        }

        CombatUnit SpawnEnemy(EnemyDefinition e, Vector3 pos)
        {
            var go = new GameObject($"Enemy_{e.id}");
            go.transform.position = pos;
            var u = go.AddComponent<CombatUnit>();
            u.InitEnemy(e, CombatTeam.Enemy, grid.BaseY);
            return u;
        }

        CombatUnit SpawnUnit(CombatUnitDefinition def, CombatTeam team, Vector3 pos)
        {
            var go = new GameObject($"{team}_{def.id}");
            go.transform.position = pos;
            var u = go.AddComponent<CombatUnit>();
            u.Init(def, team, grid.BaseY);
            return u;
        }

        CombatUnit SpawnCharacter(CharacterDefinition ch, CombatTeam team, Vector3 pos)
        {
            var go = new GameObject($"{team}_{ch.id}");
            go.transform.position = pos;
            var u = go.AddComponent<CombatUnit>();
            u.InitCharacter(ch, team, grid.BaseY);
            return u;
        }

        // ── Deploy ────────────────────────────────────────────────────────────

        public void SelectArcher()   { _selectedSlot = DeploySlot.Archer; ClearUnitSelection(); }
        public void SelectHorseman() { _selectedSlot = DeploySlot.Horseman; ClearUnitSelection(); }

        public void OnFieldTapped(Vector3 worldPos)
        {
            if (State != BattleState.Fighting || grid == null) return;

            int col = grid.WorldToColumn(worldPos);
            int row = grid.WorldToRow(worldPos);
            var cell = new Vector2Int(col, row);

            // Tapping on/near a deployed unit selects it (to use its manual skill) instead of
            // deploying. Uses actual position so advancing units stay tappable after they move.
            var existing = NearestPlayerUnitNear(worldPos, grid.cellSize * 0.6f);
            if (existing != null)
            {
                SelectUnit(existing);
                return;
            }
            if (_playerCells.ContainsKey(cell)) return;   // cell already holds a unit

            bool isArcher = _selectedSlot == DeploySlot.Archer;
            if (isArcher && _archersLeft <= 0) return;
            if (!isArcher && _horsemenLeft <= 0) return;

            var ch = isArcher ? archerCharacter : horsemanCharacter;
            var def = isArcher ? archerDef : horsemanDef;
            Vector3 at = grid.CellCenter(col, row);
            CombatUnit u = ch != null ? SpawnCharacter(ch, CombatTeam.Player, at)
                                      : (def != null ? SpawnUnit(def, CombatTeam.Player, at) : null);
            if (u == null) return;

            u.cell = cell;
            u.occupiesCell = true;
            _playerCells[cell] = u;
            ClearUnitSelection();

            if (isArcher) _archersLeft--; else _horsemenLeft--;
            UpdateHud();
        }

        // ── Manual skill selection ──────────────────────────────────────────────

        CombatUnit NearestPlayerUnitNear(Vector3 worldPos, float maxDist)
        {
            CombatUnit best = null;
            float bestSq = maxDist * maxDist;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u.team != CombatTeam.Player) continue;
                float dSq = ((Vector2)(u.transform.position - worldPos)).sqrMagnitude;
                if (dSq <= bestSq) { bestSq = dSq; best = u; }
            }
            return best;
        }

        void SelectUnit(CombatUnit u)
        {
            _selectedUnit = u;
            if (hud == null) return;
            if (u.HasManualSkill) hud.ShowSkillButton(u.SkillName, u.SkillReady);
            else hud.HideSkillButton();
        }

        void ClearUnitSelection()
        {
            _selectedUnit = null;
            if (hud != null) hud.HideSkillButton();
        }

        /// <summary>Wired to the HUD skill button: fire the selected unit's manual skill.</summary>
        public void UseSelectedUnitSkill()
        {
            if (_selectedUnit == null) return;
            if (_selectedUnit.TryActivateManualSkill() && hud != null)
                hud.SetSkillButtonReady(false);
        }

        public void DamageBase(float dmg)
        {
            _baseHP -= dmg;
            UpdateHud();
            if (_baseHP <= 0f) EndBattle(false);
        }

        void CheckWin()
        {
            bool allSpawned = _schedule == null || _spawnIndex >= _schedule.Count;
            if (State == BattleState.Fighting && allSpawned && _enemiesAlive <= 0)
                EndBattle(true);
        }

        void EndBattle(bool won)
        {
            State = won ? BattleState.Won : BattleState.Lost;
            if (won && HerdManager.Instance != null)
            {
                HerdManager.Instance.Add("sheep", level.rewardSheep);
                HerdManager.Instance.Add("cattle", level.rewardCattle);
                HerdManager.Instance.Add("special_horse", level.rewardHorse);
                SaveSystem.Save();
            }
            if (hud != null)
                hud.OnBattleEnded(won, level.rewardSheep, level.rewardCattle, level.rewardHorse);
        }

        public void Return()
        {
            ClearUnits();
            State = BattleState.Idle;
            if (_cam != null)
            {
                _cam.transform.position = _savedCamPos;
                _cam.orthographicSize = _savedCamSize;
            }
            if (hud != null) hud.OnReturned();
        }

        void ClearUnits()
        {
            for (int i = _units.Count - 1; i >= 0; i--)
                if (_units[i] != null) Destroy(_units[i].gameObject);
            _units.Clear();
            _playerCells.Clear();
        }

        // ── Camera ────────────────────────────────────────────────────────────

        void FrameCamera()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            _savedCamPos = _cam.transform.position;
            _savedCamSize = _cam.orthographicSize;

            float w = level.gridWidth * level.cellSize;
            float h = level.gridHeight * level.cellSize;
            float sizeForWidth = (w / 2f + 1f) / (9f / 16f);
            float sizeForHeight = h / 2f + 1f;

            _cam.transform.position = new Vector3(battleAnchor.x, grid.CenterY, _savedCamPos.z);
            _cam.orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight);
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public CombatUnit NearestOpponentWithin(CombatUnit asker, float range)
        {
            CombatUnit best = null, bestTaunt = null;
            float bestSq = range * range, bestTauntSq = range * range;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u == asker || u.team == asker.team) continue;
                float dSq = ((Vector2)(u.transform.position - asker.transform.position)).sqrMagnitude;
                if (dSq <= bestSq) { bestSq = dSq; best = u; }
                if (u.IsTaunting() && dSq <= bestTauntSq) { bestTauntSq = dSq; bestTaunt = u; }
            }
            return bestTaunt != null ? bestTaunt : best;   // taunters pull aggro
        }

        /// <summary>All opponents of <paramref name="asker"/> within range, nearest first (fills the supplied list).</summary>
        public void OpponentsWithin(CombatUnit asker, float range, List<CombatUnit> results)
        {
            results.Clear();
            float rSq = range * range;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u == asker || u.team == asker.team) continue;
                if (((Vector2)(u.transform.position - asker.transform.position)).sqrMagnitude <= rSq)
                    results.Add(u);
            }
            SortByDistance(results, asker.transform.position);
        }

        public void AlliesWithin(CombatUnit asker, float radius, bool includeSelf, List<CombatUnit> results)
        {
            results.Clear();
            float rSq = radius * radius;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u.team != asker.team) continue;
                if (u == asker) { if (includeSelf) results.Add(u); continue; }
                if (((Vector2)(u.transform.position - asker.transform.position)).sqrMagnitude <= rSq)
                    results.Add(u);
            }
        }

        public bool AnyAllyWounded(CombatUnit asker, float radius, float fractionThreshold)
        {
            var u = MostWoundedAlly(asker, radius, true);
            return u != null && u.HealthFraction < fractionThreshold;
        }

        public CombatUnit MostWoundedAlly(CombatUnit asker, float radius, bool includeSelf)
        {
            CombatUnit best = null;
            float worst = 1f;
            _woundedScratch.Clear();
            AlliesWithin(asker, radius, includeSelf, _woundedScratch);
            for (int i = 0; i < _woundedScratch.Count; i++)
            {
                float f = _woundedScratch[i].HealthFraction;
                if (f < worst) { worst = f; best = _woundedScratch[i]; }
            }
            return best;
        }

        static void SortByDistance(List<CombatUnit> list, Vector3 from)
        {
            list.Sort((a, b) =>
                ((Vector2)(a.transform.position - from)).sqrMagnitude
                .CompareTo(((Vector2)(b.transform.position - from)).sqrMagnitude));
        }

        private readonly List<CombatUnit> _query = new List<CombatUnit>();
        private readonly List<CombatUnit> _woundedScratch = new List<CombatUnit>();

        // ── Ability resolution ──────────────────────────────────────────────────

        /// <summary>Gather the ability's targets and apply its primary (and optional secondary) effect.</summary>
        public void ResolveAbility(CombatUnit caster, Ability ab)
        {
            if (caster == null || ab == null || !ab.HasEffect) return;

            _query.Clear();
            GatherTargets(caster, ab, _query);
            for (int i = 0; i < _query.Count; i++)
                ApplyEffect(caster, _query[i], ab.effect, ab.magnitude, ab.duration);

            if (ab.HasSecondary)
                for (int i = 0; i < _query.Count; i++)
                    ApplyEffect(caster, _query[i], ab.secondaryEffect, ab.secondaryMagnitude, ab.secondaryDuration);
        }

        void GatherTargets(CombatUnit caster, Ability ab, List<CombatUnit> results)
        {
            float radius = ab.radius > 0f ? ab.radius : 9999f;
            switch (ab.target)
            {
                case EffectTarget.SelfOnly:
                    results.Add(caster);
                    break;
                case EffectTarget.AllAllies:
                case EffectTarget.AlliesInRadius:
                    GatherUnitsInShape(caster, ShapeFor(ab, allyTarget: true), sameTeam: true, results);
                    break;
                case EffectTarget.MostWoundedAlly:
                    var w = MostWoundedAlly(caster, radius, true);
                    if (w != null) results.Add(w);
                    break;
                case EffectTarget.SingleEnemy:
                    var e = NearestOpponentWithin(caster, radius);
                    if (e != null) results.Add(e);
                    break;
                case EffectTarget.EnemiesInRadius:
                    GatherUnitsInShape(caster, ShapeFor(ab, allyTarget: false), sameTeam: false, results);
                    break;
            }
        }

        /// <summary>The ability's authored AreaShape, or a legacy circle of its radius if none.</summary>
        AreaShape ShapeFor(Ability ab, bool allyTarget)
        {
            if (ab.useCustomShape && ab.area != null) return ab.area;
            return new AreaShape
            {
                shape = AoeShape.Circle,
                anchor = allyTarget ? ShapeAnchor.Caster : ShapeAnchor.TargetEnemy,
                radius = ab.radius > 0f ? ab.radius : 9999f
            };
        }

        /// <summary>
        /// Collect every unit of the chosen side that falls inside the area shape, resolved
        /// in continuous world space (circle/cone/line). The grid is only for placement.
        /// </summary>
        public void GatherUnitsInShape(CombatUnit caster, AreaShape area, bool sameTeam, List<CombatUnit> results)
        {
            if (caster == null || area == null) return;

            var focus = NearestOpponentWithin(caster, 9999f);
            Vector2 casterPos = caster.transform.position;
            Vector2 origin = (area.anchor == ShapeAnchor.TargetEnemy && focus != null)
                ? (Vector2)focus.transform.position
                : casterPos;
            Vector2 dir = ShapeDirection2D(caster, area, focus);

            float rSq = area.radius * area.radius;
            float cosHalfAngle = Mathf.Cos(Mathf.Deg2Rad * area.coneAngle * 0.5f);
            float halfWidthSq = (area.lineWidth * 0.5f) * (area.lineWidth * 0.5f);

            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null) continue;
                bool ally = u.team == caster.team;
                if (sameTeam != ally) continue;

                Vector2 to = (Vector2)u.transform.position - origin;
                switch (area.shape)
                {
                    case AoeShape.Circle:
                        if (to.sqrMagnitude <= rSq) results.Add(u);
                        break;
                    case AoeShape.Cone:
                    {
                        float d = to.magnitude;
                        if (d > area.radius) break;
                        if (d < 0.0001f) { results.Add(u); break; }
                        if (Vector2.Dot(to / d, dir) >= cosHalfAngle) results.Add(u);
                        break;
                    }
                    case AoeShape.Line:
                    {
                        float t = Vector2.Dot(to, dir);          // distance along the line
                        if (t < 0f || t > area.radius) break;
                        Vector2 perp = to - dir * t;             // offset from the line's spine
                        if (perp.sqrMagnitude <= halfWidthSq) results.Add(u);
                        break;
                    }
                }
            }
        }

        Vector2 ShapeDirection2D(CombatUnit caster, AreaShape area, CombatUnit focus)
        {
            if (area.direction == ShapeDirection.TowardNearestEnemy && focus != null)
            {
                Vector2 d = (Vector2)(focus.transform.position - caster.transform.position);
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            // Forward: player units face up the grid, enemies face down toward the base.
            return caster.team == CombatTeam.Player ? Vector2.up : Vector2.down;
        }

        void ApplyEffect(CombatUnit caster, CombatUnit target, EffectType effect, float magnitude, float duration)
        {
            if (target == null) return;
            switch (effect)
            {
                case EffectType.AoeDamage:        target.TakeDamage(magnitude); break;
                case EffectType.ArmorPierce:      target.TakePureDamage(magnitude); break;
                case EffectType.Heal:             target.Heal(magnitude); break;
                case EffectType.Shield:           target.AddShield(magnitude, duration); break;
                case EffectType.HealOverTime:     target.AddEffect(EffectType.HealOverTime, magnitude, duration <= 0f ? 3f : duration); break;
                case EffectType.Stun:             target.AddEffect(EffectType.Stun, 0f, duration); break;
                case EffectType.Slow:             target.AddEffect(EffectType.Slow, magnitude, duration); break;
                case EffectType.DamageBoost:
                case EffectType.AttackSpeedBoost:
                case EffectType.DamageReduction:  target.AddEffect(effect, magnitude, duration <= 0f ? 5f : duration); break;
                case EffectType.Taunt:            caster.AddEffect(EffectType.Taunt, 0f, duration); break; // the caster taunts
                default: break; // MultiShot is a passive talent, not an applied effect
            }
        }

        void UpdateHud()
        {
            if (hud == null) return;
            int remaining = (_schedule != null ? _schedule.Count - _spawnIndex : 0) + _enemiesAlive;
            hud.Refresh(_baseHP, level != null ? level.baseMaxHP : 0f, remaining, _archersLeft, _horsemenLeft);
        }
    }

    /// <summary>Sits on the battle grid collider and forwards taps as deploy requests.</summary>
    public class BattleFieldInput : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData e)
        {
            if (BattleController.Instance == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(e.position.x, e.position.y, -cam.transform.position.z));
            w.z = 0f;
            BattleController.Instance.OnFieldTapped(w);
        }
    }
}
