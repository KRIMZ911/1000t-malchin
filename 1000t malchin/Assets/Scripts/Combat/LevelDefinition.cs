using System.Collections.Generic;
using UnityEngine;

namespace Malchin.Combat
{
    /// <summary>One scheduled enemy: which type, when (seconds after start), and in which column.</summary>
    [System.Serializable]
    public class EnemySpawn
    {
        public CombatUnitDefinition enemy;
        [Min(0f)] public float time;
        [Min(0)] public int column;
    }

    /// <summary>
    /// A battle level as data: the battlefield grid size and a timeline of enemy
    /// spawns. Edit these as assets (custom inspector) or generate them in code.
    /// Create via: Assets > Create > Malchin > Level Definition
    /// </summary>
    [CreateAssetMenu(menuName = "Malchin/Level Definition", fileName = "Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string levelName = "New Level";

        [Header("Battlefield grid")]
        [Min(1)] public int gridWidth = 6;    // columns
        [Min(1)] public int gridHeight = 8;   // rows (enemies enter the top, march to row 0)
        [Min(0.25f)] public float cellSize = 1f;

        [Header("Base")]
        [Min(1f)] public float baseMaxHP = 10f;

        [Header("Spawn timeline")]
        public List<EnemySpawn> spawns = new List<EnemySpawn>();

        [Header("Reward on win")]
        public int rewardSheep = 40;
        public int rewardCattle = 6;
        public int rewardHorse = 1;

        public int TotalEnemies => spawns.Count;

        public float Duration
        {
            get
            {
                float max = 0f;
                foreach (var s in spawns) if (s.time > max) max = s.time;
                return max;
            }
        }

        /// <summary>Spawns sorted by time (a copy; does not mutate the asset).</summary>
        public List<EnemySpawn> SortedSpawns()
        {
            var copy = new List<EnemySpawn>(spawns);
            copy.Sort((a, b) => a.time.CompareTo(b.time));
            return copy;
        }
    }
}
