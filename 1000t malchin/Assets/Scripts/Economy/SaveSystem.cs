using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Malchin.Economy
{
    /// <summary>
    /// Saves and loads game state as JSON in Application.persistentDataPath.
    /// Call SaveSystem.Save() / SaveSystem.Load() from any script.
    /// </summary>
    public static class SaveSystem
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "save.json");

        [System.Serializable]
        private class SaveData
        {
            public List<LivestockEntry> livestock = new List<LivestockEntry>();
        }

        [System.Serializable]
        private class LivestockEntry
        {
            public string id;
            public float count;
        }

        public static void Save()
        {
            var herd = HerdManager.Instance;
            if (herd == null) { Debug.LogWarning("SaveSystem: HerdManager not found."); return; }

            var data = new SaveData();
            foreach (var kv in herd.GetRawCounts())
                data.livestock.Add(new LivestockEntry { id = kv.Key, count = kv.Value });

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
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

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);

            var dict = new Dictionary<string, float>();
            foreach (var entry in data.livestock)
                dict[entry.id] = entry.count;

            herd.LoadCounts(dict);
            Debug.Log("Game loaded.");
        }

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
