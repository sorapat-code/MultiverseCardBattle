using System;
using System.Collections.Generic;

namespace MultiverseCardBattle.Data
{
    [Serializable]
    public class OwnedCardEntry
    {
        public string CardID;
        public int Count;
    }

    [Serializable]
    public class DeckList
    {
        public string Name;

        // การ์ดในเด็ค ใส่ ID ซ้ำตามจำนวนใบ
        public List<string> CardIDs = new List<string>();

        public int CountOf(string cardId)
        {
            int count = 0;

            foreach (string id in CardIDs)
            {
                if (id == cardId)
                    count++;
            }

            return count;
        }
    }

    /// <summary>ภารกิจรายวันที่สุ่มได้วันนี้ และความคืบหน้า</summary>
    [Serializable]
    public class DailyQuestState
    {
        public string Id;
        public int Progress;
        public bool Claimed;
    }

    /// <summary>ค่าความชำนาญ (Mastery) ของการ์ดแต่ละใบ</summary>
    [Serializable]
    public class MasteryEntry
    {
        public string CardID;
        public int Xp;

        // เลเวลสูงสุดที่แจกรางวัลไปแล้ว
        public int RewardedLevel;
    }

    /// <summary>
    /// ข้อมูลที่บันทึกลงไฟล์ (JsonUtility)
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 1 = ก่อนมี Gold / 2 = มี Gold และตั้งค่าเสียง / 3 = มี Dust และ Achievement
        // 4 = มี Daily Login / Daily Quest / Mastery / Milestone
        public const int CurrentVersion = 4;

        public int Version = CurrentVersion;

        public List<OwnedCardEntry> Owned = new List<OwnedCardEntry>();
        public List<DeckList> Decks = new List<DeckList>();
        public int ActiveDeckIndex;

        public List<string> CompletedStages = new List<string>();

        // เหรียญสำหรับซื้อแพ็คการ์ด ได้จากการชนะการต่อสู้
        public int Gold;

        public bool SoundMuted;

        // ตั้งค่า (Options)
        public float MasterVolume = 1f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        public bool Fullscreen = true;
        public bool VSync = true;
        public int FpsLimit;        // 0 = ไม่จำกัด
        public float AnimSpeed = 1f;
        public int AiSpeed = 1;     // 0 = ช้า, 1 = ปกติ, 2 = เร็ว

        // ผงสำหรับคราฟการ์ด ได้จากการย่อยการ์ดและรางวัล Achievement
        public int Dust;

        // สถิติสำหรับ Achievement
        public int StatBattles;
        public int StatWins;
        public int StatPacks;
        public int StatCrafted;
        public int StatDisenchanted;
        public int StatSecrets;
        public int StatDeckOutWins;
        public int StatComebackWins;
        public int StatFlawlessWins;
        public int StatFastWins;

        // Achievement ที่ปลดล็อกแล้ว (รางวัลถูกแจกตอนปลดล็อก)
        public List<string> Achievements = new List<string>();

        // Daily Login: วันที่รับล่าสุด (yyyy-MM-dd) และจำนวนวันติดต่อกัน
        public string LoginLastClaim = "";
        public int LoginStreak;

        // Daily Quest: วันที่ของชุดภารกิจนี้ + สถานะ + รับโบนัสครบทุกอันแล้วหรือยัง
        public string QuestDate = "";
        public List<DailyQuestState> Quests = new List<DailyQuestState>();
        public bool QuestBonusClaimed;

        // Mastery ของการ์ด Epic ขึ้นไป
        public List<MasteryEntry> Mastery = new List<MasteryEntry>();

        // Collection Milestone ที่รับรางวัลแล้ว
        public List<string> ClaimedMilestones = new List<string>();

        // Story Mode: บทที่รับรางวัลจบบทแล้ว ("chapter_1" ...)
        public List<string> StoryChaptersRewarded = new List<string>();
    }

    /// <summary>
    /// โปรไฟล์ผู้เล่น: คอลเลกชัน, เด็ค, ความคืบหน้า Campaign
    /// </summary>
    public class PlayerProfile
    {
        public const int MaxDecks = 9;
        public const int StartingGold = 150;

        public SaveData Data;

        public PlayerProfile(SaveData data)
        {
            Data = data ?? new SaveData();
        }

        public static PlayerProfile CreateNew()
        {
            SaveData data = new SaveData();

            PlayerProfile profile = new PlayerProfile(data);

            foreach (string id in StarterDecks.StarterCollectionIds())
                profile.AddCard(id, 2);

            data.Gold = StartingGold;

            DeckList starter = profile.AddDeck("Starter Deck");

            starter.CardIDs.AddRange(StarterDecks.PlayerStarterDeckIds());

            return profile;
        }

        // ---------- คอลเลกชัน ----------

        public IReadOnlyList<OwnedCardEntry> Owned
        {
            get { return Data.Owned; }
        }

        public int GetOwnedCount(string cardId)
        {
            foreach (OwnedCardEntry entry in Data.Owned)
            {
                if (entry.CardID == cardId)
                    return entry.Count;
            }

            return 0;
        }

        public void AddCard(string cardId, int count)
        {
            if (string.IsNullOrEmpty(cardId) || count <= 0)
                return;

            foreach (OwnedCardEntry entry in Data.Owned)
            {
                if (entry.CardID == cardId)
                {
                    entry.Count += count;
                    return;
                }
            }

            OwnedCardEntry created = new OwnedCardEntry();

            created.CardID = cardId;
            created.Count = count;

            Data.Owned.Add(created);
        }

        /// <summary>เอาการ์ดออกจากคอลเลกชัน (ไม่ต่ำกว่า 0) คืนจำนวนที่เอาออกจริง</summary>
        public int RemoveCard(string cardId, int count)
        {
            if (string.IsNullOrEmpty(cardId) || count <= 0)
                return 0;

            foreach (OwnedCardEntry entry in Data.Owned)
            {
                if (entry.CardID != cardId)
                    continue;

                int removed = Math.Min(count, entry.Count);

                entry.Count -= removed;

                if (entry.Count <= 0)
                    Data.Owned.Remove(entry);

                return removed;
            }

            return 0;
        }

        // จำนวนใบมากที่สุดของการ์ดนี้ที่เด็คใบใดใบหนึ่งใช้อยู่
        public int MaxCopiesInDecks(string cardId)
        {
            int most = 0;

            foreach (DeckList deck in Data.Decks)
                most = Math.Max(most, deck.CountOf(cardId));

            return most;
        }

        // ---------- เด็ค ----------

        public IReadOnlyList<DeckList> Decks
        {
            get { return Data.Decks; }
        }

        public DeckList ActiveDeck
        {
            get
            {
                if (Data.Decks.Count == 0)
                    return null;

                if (Data.ActiveDeckIndex < 0 ||
                    Data.ActiveDeckIndex >= Data.Decks.Count)
                    Data.ActiveDeckIndex = 0;

                return Data.Decks[Data.ActiveDeckIndex];
            }
        }

        public void SetActiveDeck(int index)
        {
            if (index >= 0 && index < Data.Decks.Count)
                Data.ActiveDeckIndex = index;
        }

        public DeckList AddDeck(string name)
        {
            if (Data.Decks.Count >= MaxDecks)
                return null;

            DeckList deck = new DeckList();

            deck.Name = string.IsNullOrEmpty(name)
                ? NextDeckName()
                : name;

            Data.Decks.Add(deck);

            return deck;
        }

        // "Deck N" ที่เลข N น้อยที่สุดที่ยังไม่มีเด็คไหนใช้ (เริ่มที่ Deck 1)
        private string NextDeckName()
        {
            for (int number = 1; ; number++)
            {
                string candidate = "Deck " + number;

                bool used = false;

                foreach (DeckList existing in Data.Decks)
                {
                    if (existing.Name == candidate)
                    {
                        used = true;
                        break;
                    }
                }

                if (!used)
                    return candidate;
            }
        }

        public bool RemoveDeck(DeckList deck)
        {
            // ต้องเหลืออย่างน้อย 1 เด็ค
            if (Data.Decks.Count <= 1)
                return false;

            int removedIndex = Data.Decks.IndexOf(deck);

            if (removedIndex < 0)
                return false;

            Data.Decks.RemoveAt(removedIndex);

            // เด็คที่ใช้งานอยู่ต้องยังเป็นเด็คเดิม แม้ลบเด็คที่อยู่ก่อนหน้ามัน
            if (removedIndex < Data.ActiveDeckIndex)
                Data.ActiveDeckIndex--;

            if (Data.ActiveDeckIndex >= Data.Decks.Count)
                Data.ActiveDeckIndex = Data.Decks.Count - 1;

            return true;
        }

        // ---------- Gold ----------

        public int Gold
        {
            get { return Data.Gold; }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Data.Gold += amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || Data.Gold < amount)
                return false;

            Data.Gold -= amount;

            return true;
        }

        // ---------- Dust ----------

        public int Dust
        {
            get { return Data.Dust; }
        }

        public void AddDust(int amount)
        {
            if (amount > 0)
                Data.Dust += amount;
        }

        public bool TrySpendDust(int amount)
        {
            if (amount < 0 || Data.Dust < amount)
                return false;

            Data.Dust -= amount;

            return true;
        }

        // ---------- Campaign ----------

        public bool IsStageCompleted(string stageId)
        {
            return Data.CompletedStages.Contains(stageId);
        }

        public void MarkStageCompleted(string stageId)
        {
            if (!Data.CompletedStages.Contains(stageId))
                Data.CompletedStages.Add(stageId);
        }
    }
}
