using System;
using System.IO;
using UnityEngine;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Core
{
    /// <summary>
    /// บันทึก/โหลดโปรไฟล์ผู้เล่นเป็นไฟล์ JSON ใน persistentDataPath
    /// ใช้ SaveManager.Profile เพื่อเข้าถึงข้อมูล และเรียก Save() หลังแก้ไข
    /// </summary>
    public static class SaveManager
    {
        private const string FileName = "save.json";

        private static PlayerProfile profile;

        public static PlayerProfile Profile
        {
            get
            {
                if (profile == null)
                    Load();

                return profile;
            }
        }

        private static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static void Load()
        {
            profile = null;

            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);

                    SaveData data = JsonUtility.FromJson<SaveData>(json);

                    if (data != null && data.Decks != null && data.Decks.Count > 0)
                        profile = new PlayerProfile(data);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("SaveManager: failed to load save: " + exception.Message);
            }

            if (profile == null)
            {
                profile = PlayerProfile.CreateNew();

                Save();
            }
        }

        public static void Save()
        {
            if (profile == null)
                return;

            try
            {
                string json = JsonUtility.ToJson(profile.Data, true);

                File.WriteAllText(FilePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("SaveManager: failed to save: " + exception.Message);
            }
        }

        public static void ResetProgress()
        {
            profile = PlayerProfile.CreateNew();

            Save();
        }
    }
}
