using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Economy
{
    /// <summary>
    /// Singleton that holds the player's current livestock counts and applies
    /// passive growth. Wire livestock definitions in the Inspector.
    /// </summary>
    public class HerdManager : MonoBehaviour
    {
        public static HerdManager Instance { get; private set; }

        [Tooltip("All livestock types that exist in the game.")]
        public List<LivestockDefinition> livestockTypes;

        [Tooltip("Seconds between each passive growth tick.")]
        public float tickIntervalSeconds = 10f;

        // Runtime state: id -> current count (float so fractional growth accumulates)
        private Dictionary<string, float> _counts = new Dictionary<string, float>();
        // Cap multipliers applied by building upgrades (id -> multiplier, default 1)
        private Dictionary<string, float> _capMultipliers = new Dictionary<string, float>();
        // Growth multipliers applied by building upgrades (id -> multiplier, default 1)
        private Dictionary<string, float> _growthMultipliers = new Dictionary<string, float>();

        private float _tickTimer;

        public event System.Action OnHerdChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            InitializeCounts();
        }

        void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickIntervalSeconds)
            {
                _tickTimer -= tickIntervalSeconds;
                ApplyGrowthTick();
            }
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
            _capMultipliers.TryGetValue(id, out float mult);
            if (mult == 0f) mult = 1f;
            return Mathf.RoundToInt(def.baseCap * mult);
        }

        /// <summary>Returns false if the herd cannot afford it.</summary>
        public bool Spend(string id, int amount)
        {
            if (GetCount(id) < amount) return false;
            _counts[id] -= amount;
            OnHerdChanged?.Invoke();
            return true;
        }

        public void Add(string id, int amount)
        {
            if (!_counts.ContainsKey(id)) return;
            _counts[id] = Mathf.Min(_counts[id] + amount, GetCap(id));
            OnHerdChanged?.Invoke();
        }

        /// <summary>Called by building upgrades to boost cap or growth rate.</summary>
        public void SetCapMultiplier(string id, float multiplier)
        {
            _capMultipliers[id] = multiplier;
        }

        public void SetGrowthMultiplier(string id, float multiplier)
        {
            _growthMultipliers[id] = multiplier;
        }

        // ── Save / load support ───────────────────────────────────────────────

        public Dictionary<string, float> GetRawCounts() => new Dictionary<string, float>(_counts);

        public void LoadCounts(Dictionary<string, float> saved)
        {
            foreach (var kv in saved)
                if (_counts.ContainsKey(kv.Key))
                    _counts[kv.Key] = kv.Value;
            OnHerdChanged?.Invoke();
        }

        // ── Internals ─────────────────────────────────────────────────────────

        void InitializeCounts()
        {
            foreach (var def in livestockTypes)
            {
                if (!_counts.ContainsKey(def.id))
                    _counts[def.id] = def.startingCount;
            }
            OnHerdChanged?.Invoke();
        }

        void ApplyGrowthTick()
        {
            bool changed = false;
            foreach (var def in livestockTypes)
            {
                int cap = GetCap(def.id);
                if (_counts[def.id] >= cap) continue;

                _growthMultipliers.TryGetValue(def.id, out float growthMult);
                if (growthMult == 0f) growthMult = 1f;

                _counts[def.id] = Mathf.Min(_counts[def.id] + def.baseGrowthPerTick * growthMult, cap);
                changed = true;
            }
            if (changed) OnHerdChanged?.Invoke();
        }

        LivestockDefinition GetDefinition(string id)
        {
            return livestockTypes.Find(d => d.id == id);
        }
    }
}
