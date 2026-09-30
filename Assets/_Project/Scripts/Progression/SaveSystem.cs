using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WannabeSkattebo
{
    [Serializable]
    public class GearLevel
    {
        public string Id;
        public int Level;
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public int Wallet;
        public int BestScore;
        public int BestDistance;
        public int TotalRuns;
        public int HighestStageUnlocked; // index into GameCatalog.Stages
        public List<GearLevel> Gear = new List<GearLevel>();

        public int GetGearLevel(string id)
        {
            foreach (var g in Gear) if (g.Id == id) return g.Level;
            return 0;
        }

        public void SetGearLevel(string id, int level)
        {
            foreach (var g in Gear)
            {
                if (g.Id == id) { g.Level = level; return; }
            }
            Gear.Add(new GearLevel { Id = id, Level = level });
        }

        public int TotalGearLevels()
        {
            int total = 0;
            foreach (var g in Gear) total += g.Level;
            return total;
        }
    }

    /// <summary>JSON save file in persistentDataPath. Path is injectable for tests.</summary>
    public class SaveSystem
    {
        readonly string _path;

        public SaveData Data { get; private set; }

        public SaveSystem(string path = null)
        {
            _path = path ?? Path.Combine(Application.persistentDataPath, "save.json");
        }

        public SaveData Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Could not read save, starting fresh: {e.Message}");
                Data = null;
            }
            Data ??= new SaveData();
            return Data;
        }

        public void Save()
        {
            if (Data == null) return;
            try
            {
                string tmp = _path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(tmp, _path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }
    }
}
