using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Battle;

namespace MultiverseCardBattle.Effects
{
    public class EffectContext
    {
        // การ์ดที่กำลังใช้ Effect
        public CardInstance SourceCard;

        // เจ้าของการ์ด
        public BattlePlayer SourcePlayer;

        // เป้าหมายของ Effect
        public CardInstance TargetCard;

        // เจ้าของเป้าหมาย
        public BattlePlayer TargetPlayer;

        // Battle ปัจจุบัน
        public BattleManager Battle;

        // ผลลัพธ์จาก Discover
        public DiscoverResult DiscoverResult;

        // แหล่งการ์ดสำหรับ Discover
        public CardPool CardPool;
    }
}