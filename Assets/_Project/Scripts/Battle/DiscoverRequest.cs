using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// Discover ที่รอให้ผู้เล่นเลือก 1 ใน N ใบ (UI แสดง แล้วเรียก BattleManager.ResolveDiscover)
    /// </summary>
    public class DiscoverRequest
    {
        public BattlePlayer Player;
        public DiscoverResult Result;
    }
}
