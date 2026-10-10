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

    /// <summary>
    /// ข้อมูลที่บันทึกลงไฟล์ (JsonUtility)
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 1 = ก่อนมี Gold / 2 = มี Gold และตั้งค่าเสียง
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;

        public List<OwnedCardEntry> Owned = new List<OwnedCardEntry>();
        public List<DeckList> Decks = new List<DeckList>();
        public int ActiveDeckIndex;

        public List<string> CompletedStages = new List<string>();

        // เหรียญสำหรับซื้อแพ็คการ์ด ได้จากการชนะการต่อสู้
        public int Gold;

        public bool SoundMuted;
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
                ? "Deck " + (Data.Decks.Count + 1)
                : name;

            Data.Decks.Add(deck);

            return deck;
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
