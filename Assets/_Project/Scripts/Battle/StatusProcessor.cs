using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// จัดการสถานะของยูนิต: Burn/Regen ตอนเริ่มเทิร์น และนับอายุสถานะตอนจบเทิร์น
    /// (ทำงานกับยูนิตของผู้เล่นที่เป็นเจ้าของเทิร์นนั้นเท่านั้น)
    /// </summary>
    public static class StatusProcessor
    {
        public static void OnTurnStart(
            BattleManager battle,
            BattlePlayer player)
        {
            foreach (CardInstance card in
                new List<CardInstance>(player.Field))
            {
                foreach (StatusEffect status in
                    new List<StatusEffect>(card.Statuses))
                {
                    if (status.Type == StatusType.Burn)
                    {
                        battle.DamageUnit(null, card, status.Value, false);
                    }
                    else if (status.Type == StatusType.Regen)
                    {
                        battle.HealUnit(null, card, status.Value);
                    }
                }
            }
        }

        public static void OnTurnEnd(
            BattleManager battle,
            BattlePlayer player)
        {
            foreach (CardInstance card in player.Field)
            {
                for (int i = card.Statuses.Count - 1; i >= 0; i--)
                {
                    StatusEffect status = card.Statuses[i];

                    if (status.Duration == StatusEffect.Permanent)
                        continue;

                    status.Duration--;

                    if (status.Duration <= 0)
                        card.Statuses.RemoveAt(i);
                }
            }
        }
    }
}
