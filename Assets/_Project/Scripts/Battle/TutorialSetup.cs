using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ข้อมูลของแมตช์สอนเล่น: เด็คผู้เล่น (จัดลำดับการ์ดบนสุดไว้) และเด็คคู่ซ้อม
    /// คู่ซ้อมลงได้แต่ยูนิต ไม่โจมตี ไม่ใช้สกิล (ดู AIPlanner.TutorialMode)
    /// </summary>
    public static class TutorialSetup
    {
        public const int EnemyHealth = 10;

        // ใบที่จั่วตามลำดับ: มือเปิด 3 ใบแรก + จั่วต้นเทิร์น 1 ใบ แล้วค่อยใบถัดไป
        // Leaf Genin (1) / Shield Guardian (2) / Lightning Bolt (1) / Raid Skirmisher (3, Rush)
        // Soul Society Scout (3, Charge) / Field Medic / Sage Scholar / Power Surge
        private static readonly string[] PlayerTopOrder =
        {
            "STR_001", "STR_002", "SPL_001", "STR_012",
            "STR_010", "STR_003", "STR_013", "SPL_004"
        };

        public static List<CardData> BuildPlayerDeck()
        {
            return StarterDecks.ToCards(StarterDecks.PlayerStarterDeckIds());
        }

        public static List<CardData> BuildEnemyDeck()
        {
            List<string> ids = new List<string>();

            for (int i = 0; i < 16; i++)
                ids.Add("STR_001");

            for (int i = 0; i < 4; i++)
                ids.Add("STR_013");

            return StarterDecks.ToCards(ids);
        }

        /// <summary>ย้ายการ์ดตามลำดับที่กำหนดขึ้นไปบนสุดของเด็คผู้เล่น (ใบไหนไม่มีก็ข้าม)</summary>
        public static void StackPlayerDeck(BattlePlayer player)
        {
            if (player == null)
                return;

            List<CardInstance> picked = new List<CardInstance>();

            foreach (string id in PlayerTopOrder)
            {
                foreach (CardInstance card in player.Deck.Cards)
                {
                    if (card.Data != null && card.Data.CardID == id && !picked.Contains(card))
                    {
                        picked.Add(card);
                        break;
                    }
                }
            }

            for (int i = picked.Count - 1; i >= 0; i--)
                player.Deck.MoveToTop(picked[i]);
        }
    }
}
