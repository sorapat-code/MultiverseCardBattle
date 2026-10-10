using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ความสามารถที่รอให้ผู้เล่นคลิกเลือกยูนิตเป้าหมาย 1 ใบ (เช่น Grasp Heart ต้นเทิร์น)
    /// UI แสดงเป้าหมายที่เลือกได้ แล้วเรียก BattleManager.ResolvePendingTarget
    /// </summary>
    public class TargetRequest
    {
        public BattlePlayer Player;
        public CardInstance Source;
        public List<CardInstance> Candidates = new List<CardInstance>();
        public string Prompt;
    }
}
