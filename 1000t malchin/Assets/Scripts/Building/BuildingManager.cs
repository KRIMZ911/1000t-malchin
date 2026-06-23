using System.Collections.Generic;
using UnityEngine;
using Malchin.Economy;

namespace Malchin.Building
{
    /// <summary>One saved building: which placed ger, its level, and its grid cell.</summary>
    [System.Serializable]
    public class BuildingSaveEntry
    {
        public string instanceId;
        public int level;
        public int gridX;
        public int gridY;
    }

    /// <summary>
    /// Tracks all placed gers, aggregates their effects onto the economy
    /// (HerdManager), runs upgrades (spending livestock), and captures/restores
    /// building state for the save system.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [Tooltip("Upgrade panel UI (optional — assigned by the setup tool).")]
        public BuildingUpgradePanel upgradePanel;

        private readonly List<GerView> _gers = new List<GerView>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            EnsureGers();
            if (GridManager.Instance != null) GridManager.Instance.Rebuild(_gers);
            ApplyAllEffects();
        }

        // ── Ger registry ──────────────────────────────────────────────────────

        /// <summary>Finds every GerView in the scene once. Safe to call repeatedly.</summary>
        void EnsureGers()
        {
            if (_gers.Count > 0) return;
            _gers.AddRange(Object.FindObjectsByType<GerView>(FindObjectsSortMode.None));
        }

        GerView Find(string instanceId) => _gers.Find(g => g.instanceId == instanceId);

        // ── Tap / move callbacks (from GerView) ───────────────────────────────

        public void OnGerTapped(GerView ger)
        {
            if (upgradePanel != null) upgradePanel.Show(ger);
        }

        public void OnGerMoved(GerView ger)
        {
            SaveSystem.Save();
        }

        // ── Upgrades ──────────────────────────────────────────────────────────

        public bool CanAfford(GerView ger)
        {
            var costs = ger.definition.CostToUpgradeFrom(ger.Level);
            if (costs == null) return false;
            var herd = HerdManager.Instance;
            if (herd == null) return false;
            foreach (var c in costs)
                if (herd.GetCount(c.livestockId) < c.amount) return false;
            return true;
        }

        /// <summary>Spends the cost and raises the ger one level. Returns false if unaffordable/maxed.</summary>
        public bool TryUpgrade(GerView ger)
        {
            if (!ger.definition.CanUpgradeFrom(ger.Level)) return false;
            if (!CanAfford(ger)) return false;

            var costs = ger.definition.CostToUpgradeFrom(ger.Level);
            var herd = HerdManager.Instance;
            foreach (var c in costs) herd.Spend(c.livestockId, c.amount);

            ger.SetLevel(ger.Level + 1);
            ApplyAllEffects();
            SaveSystem.Save();
            return true;
        }

        // ── Effects ───────────────────────────────────────────────────────────

        /// <summary>
        /// Recomputes every livestock multiplier from scratch by combining all
        /// gers' current effects, then pushes them to the HerdManager.
        /// </summary>
        public void ApplyAllEffects()
        {
            EnsureGers();
            var herd = HerdManager.Instance;
            if (herd == null) return;

            var growth = new Dictionary<string, float>();
            var cap = new Dictionary<string, float>();

            foreach (var ger in _gers)
            {
                var def = ger.definition;
                if (def == null || def.effectType == GerEffectType.None) continue;
                if (string.IsNullOrEmpty(def.effectTargetLivestockId)) continue;

                float value = def.GetLevel(ger.Level).effectValue;
                var table = def.effectType == GerEffectType.GrowthMultiplier ? growth : cap;
                table.TryGetValue(def.effectTargetLivestockId, out float current);
                table[def.effectTargetLivestockId] = (current == 0f ? 1f : current) * value;
            }

            foreach (var kv in growth) herd.SetGrowthMultiplier(kv.Key, kv.Value);
            foreach (var kv in cap) herd.SetCapMultiplier(kv.Key, kv.Value);
        }

        // ── Save / load ───────────────────────────────────────────────────────

        public List<BuildingSaveEntry> CaptureState()
        {
            EnsureGers();
            var list = new List<BuildingSaveEntry>();
            foreach (var ger in _gers)
            {
                list.Add(new BuildingSaveEntry
                {
                    instanceId = ger.instanceId,
                    level = ger.Level,
                    gridX = ger.originCell.x,
                    gridY = ger.originCell.y
                });
            }
            return list;
        }

        /// <param name="restorePositions">
        /// Only restore grid cells from saves new enough to have them; older saves
        /// keep the default scene placement.
        /// </param>
        public void RestoreState(List<BuildingSaveEntry> entries, bool restorePositions)
        {
            EnsureGers();
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    var ger = Find(e.instanceId);
                    if (ger == null) continue;
                    ger.SetLevel(e.level);
                    if (restorePositions) ger.originCell = new Vector2Int(e.gridX, e.gridY);
                }
            }
            if (GridManager.Instance != null) GridManager.Instance.Rebuild(_gers);
            ApplyAllEffects();
        }
    }
}
