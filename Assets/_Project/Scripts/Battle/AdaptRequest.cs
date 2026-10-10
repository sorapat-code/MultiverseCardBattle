using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// Adapt ที่รอให้ผู้เล่นเลือก 1 ใน N ความสามารถ (UI แสดง แล้วเรียก BattleManager.ResolveAdapt)
    /// </summary>
    public class AdaptRequest
    {
        public BattlePlayer Player;
        public CardInstance Card;
        public List<AdaptOption> Options = new List<AdaptOption>();
    }
}
