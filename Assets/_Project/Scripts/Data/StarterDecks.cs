using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    public static class StarterDecks
    {
        /// <summary>
        /// เด็คตั้งต้นของผู้เล่นใหม่ (30 ใบ) ใช้เฉพาะการ์ดที่ผู้เล่นใหม่มีให้
        /// </summary>
        public static List<string> PlayerStarterDeckIds()
        {
            return Build(
                ("STR_001", 2), ("STR_002", 2), ("STR_003", 2), ("STR_004", 2),
                ("STR_006", 2), ("STR_007", 2), ("STR_009", 2), ("STR_010", 2),
                ("STR_013", 2), ("STR_014", 2), ("STR_017", 2), ("STR_018", 2),
                ("SPL_001", 2), ("SPL_002", 2), ("SPL_003", 1), ("SPL_005", 1));
        }

        /// <summary>
        /// ID การ์ดที่ผู้เล่นใหม่เริ่มต้นด้วย (ไม่ซ้ำ) จำนวนใบละ 2
        /// </summary>
        public static List<string> StarterCollectionIds()
        {
            List<string> unique = new List<string>();

            foreach (string id in PlayerStarterDeckIds())
            {
                if (!unique.Contains(id))
                    unique.Add(id);
            }

            return unique;
        }

        public static List<CardData> CreatePlayerStarterDeck()
        {
            return ToCards(PlayerStarterDeckIds());
        }

        public static List<CardData> ToCards(IEnumerable<string> ids)
        {
            List<CardData> cards = new List<CardData>();

            foreach (string id in ids)
            {
                CardData data = CardRegistry.Get(id);

                if (data != null)
                    cards.Add(data);
            }

            return cards;
        }

        /// <summary>
        /// สร้างรายการ ID จากคู่ (ID, จำนวนใบ)
        /// </summary>
        public static List<string> Build(params (string id, int count)[] entries)
        {
            List<string> ids = new List<string>();

            foreach ((string id, int count) in entries)
            {
                for (int i = 0; i < count; i++)
                    ids.Add(id);
            }

            return ids;
        }
    }
}
