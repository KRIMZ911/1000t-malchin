using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Malchin.Economy;

namespace Malchin.Combat
{
    public enum BattleState { Idle, Fighting, Won, Lost }

    /// <summary>
    /// Runs a single-lane battle off in its own world region. The camera pans
    /// there on StartBattle and back on Return. Spawns enemy waves from the top,
    /// lets the player deploy units, resolves win/lose, and rewards livestock.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public static BattleController Instance { get; private set; }
        private static readonly List<CombatUnit> _units = new List<CombatUnit>();

        [Header("Unit definitions")]
        public CombatUnitDefinition archerDef;
        public CombatUnitDefinition horsemanDef;
        public CombatUnitDefinition enemyDef;

        [Header("Battlefield (world)")]
        public Vector3 battleAnchor = new Vector3(100f, 0f, 0f);
        public float laneHalfHeight = 6f;   // base at anchor.y - this; enemies spawn at anchor.y + this
        public float battleCamSize = 7.5f;

        [Header("Wave")]
        public int enemyCount = 8;
        public float spawnInterval = 1.3f;
        public float baseMaxHP = 10f;

        [Header("Deploy limits")]
        public int archerCount = 4;
        public int horsemanCount = 4;

        [Header("Reward on win")]
        public int rewardSheep = 40;
        public int rewardCattle = 6;
        public int rewardHorse = 1;

        public BattleHUD hud;

        public BattleState State { get; private set; } = BattleState.Idle;
        public bool IsFighting => State == BattleState.Fighting;

        private float BaseY => battleAnchor.y - laneHalfHeight;
        private float SpawnY => battleAnchor.y + laneHalfHeight;
        private float LaneX => battleAnchor.x;

        private float _baseHP;
        private int _enemiesToSpawn;
        private int _enemiesAlive;
        private float _spawnTimer;
        private int _archersLeft, _horsemenLeft;
        private CombatUnitDefinition _selected;

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

        public static void Register(CombatUnit u)
        {
            if (!_units.Contains(u)) _units.Add(u);
        }

        public static void Unregister(CombatUnit u)
        {
            _units.Remove(u);
            if (Instance != null) Instance.OnUnitRemoved(u);
        }

        void OnUnitRemoved(CombatUnit u)
        {
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
            ClearUnits();

            State = BattleState.Fighting;
            _baseHP = baseMaxHP;
            _enemiesToSpawn = enemyCount;
            _enemiesAlive = 0;
            _spawnTimer = 0.5f;
            _archersLeft = archerCount;
            _horsemenLeft = horsemanCount;
            _selected = archerDef;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                _savedCamPos = _cam.transform.position;
                _savedCamSize = _cam.orthographicSize;
                _cam.transform.position = new Vector3(LaneX, battleAnchor.y, _savedCamPos.z);
                _cam.orthographicSize = battleCamSize;
            }

            if (hud != null) hud.OnBattleStarted();
            UpdateHud();
        }

        void Update()
        {
            if (State != BattleState.Fighting) return;
            if (_enemiesToSpawn > 0)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f) { SpawnEnemy(); _spawnTimer = spawnInterval; }
            }
        }

        void SpawnEnemy()
        {
            _enemiesToSpawn--;
            _enemiesAlive++;
            float x = LaneX + Random.Range(-0.6f, 0.6f);
            SpawnUnit(enemyDef, CombatTeam.Enemy, new Vector3(x, SpawnY, 0f));
            UpdateHud();
        }

        CombatUnit SpawnUnit(CombatUnitDefinition def, CombatTeam team, Vector3 pos)
        {
            var go = new GameObject($"{team}_{def.id}");
            go.transform.position = pos;
            var u = go.AddComponent<CombatUnit>();
            u.Init(def, team, BaseY);
            return u;
        }

        // ── Deploy ────────────────────────────────────────────────────────────

        public void SelectArcher() { _selected = archerDef; }
        public void SelectHorseman() { _selected = horsemanDef; }

        public void OnFieldTapped(Vector3 worldPos)
        {
            if (State != BattleState.Fighting || _selected == null) return;
            bool isArcher = _selected == archerDef;
            if (isArcher && _archersLeft <= 0) return;
            if (!isArcher && _horsemenLeft <= 0) return;

            float y = Mathf.Clamp(worldPos.y, BaseY + 0.5f, battleAnchor.y + 2f);
            SpawnUnit(_selected, CombatTeam.Player, new Vector3(LaneX, y, 0f));
            if (isArcher) _archersLeft--; else _horsemenLeft--;
            UpdateHud();
        }

        public void DamageBase(float dmg)
        {
            _baseHP -= dmg;
            UpdateHud();
            if (_baseHP <= 0f) EndBattle(false);
        }

        void CheckWin()
        {
            if (State == BattleState.Fighting && _enemiesToSpawn <= 0 && _enemiesAlive <= 0)
                EndBattle(true);
        }

        void EndBattle(bool won)
        {
            State = won ? BattleState.Won : BattleState.Lost;
            if (won && HerdManager.Instance != null)
            {
                HerdManager.Instance.Add("sheep", rewardSheep);
                HerdManager.Instance.Add("cattle", rewardCattle);
                HerdManager.Instance.Add("special_horse", rewardHorse);
                SaveSystem.Save();
            }
            if (hud != null) hud.OnBattleEnded(won, rewardSheep, rewardCattle, rewardHorse);
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
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public CombatUnit NearestOpponentWithin(CombatUnit asker, float range)
        {
            CombatUnit best = null;
            float bestSq = range * range;
            for (int i = 0; i < _units.Count; i++)
            {
                var u = _units[i];
                if (u == null || u == asker || u.team == asker.team) continue;
                float dSq = ((Vector2)(u.transform.position - asker.transform.position)).sqrMagnitude;
                if (dSq <= bestSq) { bestSq = dSq; best = u; }
            }
            return best;
        }

        void UpdateHud()
        {
            if (hud != null)
                hud.Refresh(_baseHP, baseMaxHP, _enemiesToSpawn + _enemiesAlive, _archersLeft, _horsemenLeft);
        }
    }

    /// <summary>Sits on the battlefield collider and forwards taps as deploy requests.</summary>
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
