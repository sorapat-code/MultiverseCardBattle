using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ข้อมูลที่ส่งให้ซีน Battle ก่อนเริ่ม (Campaign / Quick Battle เป็นผู้ตั้งค่า)
    /// ถ้าไม่ตั้งค่า BattleManager จะใช้เด็คตั้งต้นทั้งสองฝั่ง
    /// </summary>
    public static class BattleSetup
    {
        public static List<CardData> PlayerDeck;
        public static List<CardData> EnemyDeck;

        public static string PlayerName;
        public static string EnemyName;

        // 0 = ใช้ค่าตั้งต้นของ BattleManager
        public static int PlayerHealth;
        public static int EnemyHealth;

        // ด่านของ Campaign ที่กำลังเล่น (null = Quick Battle / ทดสอบ)
        public static CampaignStage Stage;

        public static void Reset()
        {
            PlayerDeck = null;
            EnemyDeck = null;

            PlayerName = null;
            EnemyName = null;

            PlayerHealth = 0;
            EnemyHealth = 0;

            Stage = null;
        }
    }
}
