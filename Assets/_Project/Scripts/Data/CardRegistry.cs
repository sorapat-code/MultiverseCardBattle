using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ทะเบียนการ์ดทั้งหมดในเกม ค้นหาด้วย CardID
    ///
    /// แหล่งข้อมูล:
    ///  1) CardData asset ในโฟลเดอร์ Resources/Cards (สร้างเอง หรือ export จากเมนู
    ///     Multiverse Card Battle > Export Starter Cards)
    ///  2) การ์ดตั้งต้นที่สร้างจากโค้ด (StarterCardLibrary) เฉพาะ ID ที่ยังไม่มี asset
    ///     ดังนั้นเกมเล่นได้ทันทีแม้ยังไม่ได้สร้าง asset ใดๆ
    /// </summary>
    public static class CardRegistry
    {
        private static List<CardData> all;
        private static Dictionary<string, CardData> byId;

        public static IReadOnlyList<CardData> All
        {
            get
            {
                Initialize();
                return all;
            }
        }

        public static void Reload()
        {
            all = null;
            byId = null;

            Initialize();
        }

        public static CardData Get(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            Initialize();

            CardData data;

            if (byId.TryGetValue(cardId, out data))
                return data;

            return null;
        }

        /// <summary>
        /// การ์ดที่ผู้เล่นใส่เด็ค/ได้จาก Discover ได้ (ไม่รวม Token และ Leader)
        /// </summary>
        public static bool IsCollectible(CardData data)
        {
            if (data == null)
                return false;

            if (data.Type == CardType.Leader)
                return false;

            if (data.Source == CardSource.Token || data.Source == CardSource.Debug)
                return false;

            if ((data.Tags & CardTag.Token) != 0)
                return false;

            return true;
        }

        public static CardPool CreateDiscoverPool()
        {
            Initialize();

            CardPool pool = new CardPool();

            foreach (CardData data in all)
            {
                if (IsCollectible(data))
                    pool.Add(data);
            }

            return pool;
        }

        private static void Initialize()
        {
            if (all != null)
                return;

            all = new List<CardData>();
            byId = new Dictionary<string, CardData>();

            CardData[] loaded = Resources.LoadAll<CardData>("Cards");

            foreach (CardData data in loaded)
                Register(data);

            foreach (CardData data in StarterCardLibrary.All)
                Register(data);
        }

        private static void Register(CardData data)
        {
            if (data == null || string.IsNullOrEmpty(data.CardID))
                return;

            // asset ที่โหลดมาก่อนมีสิทธิ์เหนือการ์ดตั้งต้นจากโค้ด
            if (byId.ContainsKey(data.CardID))
                return;

            byId[data.CardID] = data;
            all.Add(data);
        }
    }
}
