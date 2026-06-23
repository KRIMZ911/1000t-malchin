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
        private CombatUnitDefinition _selected;
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
            grid.Configure(level.gridWidth, level.gridHeight, level.cellSize);

            State = BattleState.Fighting;
            _baseHP = level.baseMaxHP;
            _elapsed = 0f;
            _spawnIndex = 0;
            _enemiesAlive = 0;
            _schedule = level.SortedSpawns();
            _archersLeft = archerCount;
            _horsemenLeft = horsemanCount;
            _selected = archerDef;
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
        }

        void SpawnFromEntry(EnemySpawn entry)
        {
            if (entry.enemy == null) return;
            _enemiesAlive++;
            int col = Mathf.Clamp(entry.column, 0, level.gridWidth - 1);
            var pos = new Vector3(grid.ColumnX(col), grid.TopY + 0.4f, 0f);
            SpawnUnit(entry.enemy, CombatTeam.Enemy, pos);
            UpdateHud();
        }

        CombatUnit SpawnUnit(CombatUnitDefinition def, CombatTeam team, Vector3 pos)
        {
            var go = new GameObject($"{team}_{def.id}");
            go.transform.position = pos;
            var u = go.AddComponent<CombatUnit>();
            u.Init(def, team, grid.BaseY);
            return u;
        }

        // ── Deploy ────────────────────────────────────────────────────────────

        public void SelectArcher() { _selected = archerDef; }
        public void SelectHorseman() { _selected = horsemanDef; }

        public void OnFieldTapped(Vector3 worldPos)
        {
            if (State != BattleState.Fighting || _selected == null || grid == null) return;

            int col = grid.WorldToColumn(worldPos);
            int row = grid.WorldToRow(worldPos);
            var cell = new Vector2Int(col, row);
            if (_playerCells.ContainsKey(cell)) return;            // one unit per cell

            bool isArcher = _selected == archerDef;
            if (isArcher && _archersLeft <= 0) return;
            if (!isArcher && _horsemenLeft <= 0) return;

            var u = SpawnUnit(_selected, CombatTeam.Player, grid.CellCenter(col, row));
            u.cell = cell;
            u.occupiesCell = true;
            _playerCells[cell] = u;

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
