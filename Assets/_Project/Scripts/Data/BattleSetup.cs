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

        // Leader ของผู้เล่น (ว่าง = ไม่มี Leader)
        public static string PlayerLeaderId;

        // Leader ของศัตรู (บอสท้ายบทในโหมดเนื้อเรื่อง ว่าง = ไม่มี)
        public static string EnemyLeaderId;

        // 0 = ใช้ค่าตั้งต้นของ BattleManager
        public static int PlayerHealth;
        public static int EnemyHealth;

        // โยนเหรียญสุ่มว่าใครเริ่มก่อน (ผู้เล่นที่เริ่มหลังได้จั่วเพิ่มและการ์ด Coin)
        // false = ผู้เล่น 1 เริ่มก่อนเสมอ (ใช้ในเทสต์)
        public static bool UseCoinFlip;

        // เปิดช่วงเปลี่ยนการ์ด (Mulligan) ก่อนเริ่มเทิร์นแรก
        public static bool UseMulligan;

        // 1 หรือ 2 = บังคับว่าใครเริ่มก่อน (ใช้กับ UseCoinFlip) 0 = สุ่ม
        public static int FirstPlayerOverride;

        // ด่านของ Campaign ที่กำลังเล่น (null = Quick Battle / ทดสอบ)
        public static CampaignStage Stage;

        // ระดับความยาก AI (ค่าเริ่มต้น Normal = พฤติกรรมเดิม)
        public static AiDifficulty Difficulty = AiDifficulty.Normal;

        // โหมดสอนเล่น: เด็คและมือเปิดถูกจัดไว้ AI ไม่ทำอะไร (ดู TutorialCoach)
        public static bool Tutorial;

        public static void Reset()
        {
            Difficulty = AiDifficulty.Normal;
            Tutorial = false;

            PlayerDeck = null;
            EnemyDeck = null;

            PlayerName = null;
            EnemyName = null;
            PlayerLeaderId = null;
            EnemyLeaderId = null;

            PlayerHealth = 0;
            EnemyHealth = 0;

            Stage = null;

            UseCoinFlip = false;
            UseMulligan = false;
            FirstPlayerOverride = 0;
        }
    }
}
