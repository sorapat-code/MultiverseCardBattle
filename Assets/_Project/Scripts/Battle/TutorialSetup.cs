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
        // Connie (1, Rush) / March 7th (2, Taunt) / Spark (1, ตีหน้า Leader) / Rock Lee (2, Rush)
        // Konohamaru / Tony Tony Chopper / Krillin / Scholar's Notes
        private static readonly string[] PlayerTopOrder =
        {
            "SUP_006", "SUP_008", "MAG_001", "SUP_002",
            "SUP_001", "SUP_004", "SUP_005", "MAG_006"
        };

        public static List<CardData> BuildPlayerDeck()
        {
            return StarterDecks.ToCards(StarterDecks.PlayerStarterDeckIds());
        }

        public static List<CardData> BuildEnemyDeck()
        {
            List<string> ids = new List<string>();

            for (int i = 0; i < 16; i++)
                ids.Add("SUP_006");

            for (int i = 0; i < 4; i++)
                ids.Add("SUP_010");

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
