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

        // LookTop: การ์ดจริงในกองที่เลือกได้ + การ์ดทั้งหมดที่เปิดดู (null = Discover ปกติ)
        public System.Collections.Generic.List<CardInstance> DeckCards;
        public System.Collections.Generic.List<CardInstance> DeckTop;

        public string Title;
    }
}
