using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Economy
{
    /// <summary>
    /// Singleton that holds the player's current livestock counts and applies
    /// passive growth — both continuously while the app runs AND in one batch
    /// for the time the app was closed (offline/idle growth). Wire livestock
    /// definitions in the Inspector.
    /// </summary>
    public class HerdManager : MonoBehaviour
    {
        public static HerdManager Instance { get; private set; }

        [Tooltip("All livestock types that exist in the game.")]
        public List<LivestockDefinition> livestockTypes;

        // Runtime state: id -> current count (float so fractional growth accumulates)
        private Dictionary<string, float> _counts = new Dictionary<string, float>();
        // Last whole-number count we reported, so we only raise events when the
        // displayed integer actually changes (avoids per-frame UI churn).
        private Dictionary<string, int> _lastIntCounts = new Dictionary<string, int>();
        // Cap multipliers applied by building upgrades (id -> multiplier, default 1)
        private Dictionary<string, float> _capMultipliers = new Dictionary<string, float>();
        // Growth multipliers applied by building upgrades (id -> multiplier, default 1)
        private Dictionary<string, float> _growthMultipliers = new Dictionary<string, float>();

        public event System.Action OnHerdChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCounts();
        }

        void Update()
        {
            // Continuous in-session growth: rate-per-minute scaled to this frame.
            ApplyGrowth(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public int GetCount(string id)
        {
            _counts.TryGetValue(id, out float val);
            return Mathf.FloorToInt(val);
        }

        public int GetCap(string id)
        {
            var def = GetDefinition(id);
            if (def == null) return 0;
            return Mathf.RoundToInt(def.baseCap * GetCapMultiplier(id));
        }

        /// <summary>Returns false if the herd cannot afford it.</summary>
        public bool Spend(string id, int amount)
        {
            if (GetCount(id) < amount) return false;
            _counts[id] -= amount;
            RaiseIfChanged();
            return true;
        }

        public void Add(string id, int amount)
        {
            if (!_counts.ContainsKey(id)) return;
            _counts[id] = Mathf.Min(_counts[id] + amount, GetCap(id));
            RaiseIfChanged();
        }

        /// <summary>Called by building upgrades to boost cap or growth rate.</summary>
        public void SetCapMultiplier(string id, float multiplier) => _capMultipliers[id] = multiplier;
        public void SetGrowthMultiplier(string id, float multiplier) => _growthMultipliers[id] = multiplier;

        /// <summary>
        /// Apply growth for time spent with the app closed. Called by SaveSystem
        /// on load with the real seconds elapsed since the last save.
        /// </summary>
        public void ApplyOfflineGrowth(double elapsedSeconds)
        {
            if (elapsedSeconds <= 0) return;
            ApplyGrowth(elapsedSeconds);
        }

        // ── Save / load support ───────────────────────────────────────────────

        public Dictionary<string, float> GetRawCounts() => new Dictionary<string, float>(_counts);

        public void LoadCounts(Dictionary<string, float> saved)
        {
            foreach (var kv in saved)
                if (_counts.ContainsKey(kv.Key))
                    _counts[kv.Key] = kv.Value;
            RaiseIfChanged(force: true);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        void InitializeCounts()
        {
            foreach (var def in livestockTypes)
            {
                if (!_counts.ContainsKey(def.id))
                {
                    _counts[def.id] = def.startingCount;
                    _lastIntCounts[def.id] = def.startingCount;
                }
            }
        }

        /// <summary>Adds growthPerMinute/60 * seconds to each type, capped.</summary>
        void ApplyGrowth(double seconds)
        {
            foreach (var def in livestockTypes)
            {
                if (!_counts.ContainsKey(def.id)) continue;
                int cap = GetCap(def.id);
                if (_counts[def.id] >= cap) continue;

                double perSecond = (def.baseGrowthPerMinute / 60.0) * GetGrowthMultiplier(def.id);
                _counts[def.id] = Mathf.Min(_counts[def.id] + (float)(perSecond * seconds), cap);
            }
            RaiseIfChanged();
        }

        /// <summary>Fires OnHerdChanged only when a displayed integer changed.</summary>
        void RaiseIfChanged(bool force = false)
        {
            bool changed = force;
            foreach (var def in livestockTypes)
            {
                int current = GetCount(def.id);
                _lastIntCounts.TryGetValue(def.id, out int last);
                if (current != last)
                {
                    _lastIntCounts[def.id] = current;
                    changed = true;
                }
            }
            if (changed) OnHerdChanged?.Invoke();
        }

        float GetCapMultiplier(string id)
        {
            _capMultipliers.TryGetValue(id, out float m);
            return m == 0f ? 1f : m;
        }

        float GetGrowthMultiplier(string id)
        {
            _growthMultipliers.TryGetValue(id, out float m);
            return m == 0f ? 1f : m;
        }

        LivestockDefinition GetDefinition(string id) => livestockTypes.Find(d => d.id == id);
    }
}
