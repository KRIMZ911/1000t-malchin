using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Malchin.Building;

namespace Malchin.Economy
{
    /// <summary>
    /// Saves and loads game state as JSON in Application.persistentDataPath.
    /// Stores a timestamp so the herd keeps growing while the app is closed,
    /// and a schema version so saves can be migrated later (e.g. local → backend).
    /// </summary>
    public static class SaveSystem
    {
        // Bump this whenever the SaveData shape changes; handle old values in Migrate().
        // v2: added buildings (ger levels + positions).
        // v3: buildings store grid cells (gridX/gridY) instead of world x/y.
        public const int SaveVersion = 3;

        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        [Serializable]
        private class SaveData
        {
            public int version;
            public long lastSavedUnixSeconds;
            public List<LivestockEntry> livestock = new List<LivestockEntry>();
            public List<BuildingSaveEntry> buildings = new List<BuildingSaveEntry>();
        }

        [Serializable]
        private class LivestockEntry
        {
            public string id;
            public float count;
        }

        public static void Save()
        {
            var herd = HerdManager.Instance;
            if (herd == null) { Debug.LogWarning("SaveSystem: HerdManager not found."); return; }

            var data = new SaveData
            {
                version = SaveVersion,
                lastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            foreach (var kv in herd.GetRawCounts())
                data.livestock.Add(new LivestockEntry { id = kv.Key, count = kv.Value });

            if (BuildingManager.Instance != null)
                data.buildings = BuildingManager.Instance.CaptureState();

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"Game saved to {SavePath}");
        }

        public static void Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("SaveSystem: No save file found, starting fresh.");
                return;
            }

            var herd = HerdManager.Instance;
            if (herd == null) { Debug.LogWarning("SaveSystem: HerdManager not found."); return; }

            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) { Debug.LogWarning("SaveSystem: save file unreadable."); return; }

            Migrate(data);

            // Restore stored counts first...
            var dict = new Dictionary<string, float>();
            foreach (var entry in data.livestock) dict[entry.id] = entry.count;
            herd.LoadCounts(dict);

            // Restore buildings (levels + positions) BEFORE offline growth, so the
            // herd grows offline at the upgraded rate the player left it at.
            // Grid cells only exist from v3 on; older saves keep default placement.
            if (BuildingManager.Instance != null)
                BuildingManager.Instance.RestoreState(data.buildings, data.version >= 3);

            // ...then apply growth for the time the app was closed.
            if (data.lastSavedUnixSeconds > 0)
            {
                long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                double elapsed = nowUnix - data.lastSavedUnixSeconds;
                if (elapsed > 0)
                {
                    herd.ApplyOfflineGrowth(elapsed);
                    Debug.Log($"Applied {elapsed:0}s of offline herd growth.");
                }
            }

            Debug.Log("Game loaded.");
        }

        /// <summary>Upgrades an older SaveData in place to the current schema.</summary>
        private static void Migrate(SaveData data)
        {
            if (data.version == SaveVersion) return;
            // Example for the future:
            //   if (data.version < 2) { /* convert fields */ }
            Debug.Log($"SaveSystem: migrated save from v{data.version} to v{SaveVersion}.");
            data.version = SaveVersion;
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
