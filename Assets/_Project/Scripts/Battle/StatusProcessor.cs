using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// จัดการสถานะของยูนิต: ความเสียหาย/ฟื้นฟูต่อเนื่องตอนเริ่มเทิร์น
    /// และนับอายุสถานะ / Evolve / กลับร่างตอนจบเทิร์น
    /// (ทำงานกับการ์ดของผู้เล่นที่เป็นเจ้าของเทิร์นนั้นเท่านั้น)
    /// </summary>
    public static class StatusProcessor
    {
        // ผลของสถานะที่ Fatigue / Lock ตอนกลับร่างเดิม
        private const int FatigueAttackPenalty = 1;
        private const int AfterRevertDuration = 2;

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
                    if (card.Zone != CardZone.Field)
                        break;

                    switch (status.Type)
                    {
                        case StatusType.Burn:
                            battle.DamageUnit(null, card, status.Value, false);

                            // ไฟทำให้น้ำแข็งละลาย
                            card.RemoveStatus(StatusType.Freeze);
                            break;

                        case StatusType.Bleed:
                            battle.DamageUnit(null, card, status.Value, false);
                            break;

                        case StatusType.Poison:
                            // พิษเป็นความเสียหายจริง ทะลุ Shield / Barrier
                            battle.DamageUnit(null, card, status.Value, true);
                            break;

                        case StatusType.Regen:
                            battle.HealUnit(null, card, status.Value);
                            break;
                    }
                }

                if (card.Zone == CardZone.Field &&
                    card.HasKeyword(Keyword.Regenerate))
                {
                    battle.HealUnit(card, card, BattleManager.RegenerateAmount);
                }

                if (card.Zone == CardZone.Field &&
                    card.HasStatus(StatusType.Blessed))
                {
                    Bless(battle, card);
                }
            }
        }

        // Blessed: ล้างสถานะเสีย 1 อย่างและฟื้น 1 HP
        private static void Bless(BattleManager battle, CardInstance card)
        {
            for (int i = 0; i < card.Statuses.Count; i++)
            {
                if (StatusInfo.IsHarmful(card.Statuses[i].Type))
                {
                    battle.Log(
                        card.Data.CardName + " is cleansed of " +
                        StatusInfo.DisplayName(card.Statuses[i].Type));

                    card.Statuses.RemoveAt(i);

                    break;
                }
            }

            battle.HealUnit(null, card, 1);
        }

        public static void OnTurnEnd(
            BattleManager battle,
            BattlePlayer player)
        {
            // การ์ดบนสนาม: นับอายุสถานะ แล้ว Evolve / กลับร่าง
            foreach (CardInstance card in
                new List<CardInstance>(player.Field))
            {
                card.TurnsOnField++;

                bool transformEnded = TickStatuses(card);

                if (transformEnded)
                    RevertForm(battle, card);

                TryEvolve(battle, card);
            }

            // การ์ดในมือ: นับอายุสถานะ Cost
            foreach (CardInstance card in
                new List<CardInstance>(player.Hand.Cards))
            {
                TickStatuses(card);
            }
        }

        // ลดอายุสถานะ คืน true ถ้า TemporaryTransformation หมดเวลาในรอบนี้
        private static bool TickStatuses(CardInstance card)
        {
            bool temporaryEnded = false;

            for (int i = card.Statuses.Count - 1; i >= 0; i--)
            {
                StatusEffect status = card.Statuses[i];

                if (status.Duration == StatusEffect.Permanent)
                    continue;

                status.Duration--;

                if (status.Duration <= 0)
                {
                    if (status.Type == StatusType.TemporaryTransformation)
                        temporaryEnded = true;

                    card.Statuses.RemoveAt(i);
                }
            }

            return temporaryEnded;
        }

        // กลับเป็นร่างเดิม คงความเสียหายที่สะสมไว้ แล้วติด Fatigue + Lock
        private static void RevertForm(BattleManager battle, CardInstance card)
        {
            if (card.OriginalData == null)
            {
                card.RemoveStatus(StatusType.Transformed);
                return;
            }

            string from = card.Data.CardName;

            // ร่าง Devil Trigger: กลับร่างแล้วคง HP (ลดเหลือเพดานของร่างปกติ) ไม่ติด Fatigue / Lock
            if ((card.Data.Passives & CardPassive.DevilTriggerForm) != 0)
            {
                int keptHealth = card.CurrentHealth;

                card.RestoreOriginalForm();
                card.EvolveTo(card.Data);

                card.RemoveStatus(StatusType.Transformed);

                card.CurrentHealth = System.Math.Min(keptHealth, card.MaxHealth);

                battle.Log(from + " reverts to " + card.Data.CardName);

                return;
            }

            int damage = card.MaxHealth - card.CurrentHealth;

            card.RestoreOriginalForm();
            card.ApplyBaseStats(card.Data);

            card.CurrentHealth = System.Math.Max(1, card.MaxHealth - damage);

            battle.Log(from + " reverts to " + card.Data.CardName);

            card.AddStatus(
                StatusType.TransformationFatigue,
                FatigueAttackPenalty,
                AfterRevertDuration);

            card.AddStatus(
                StatusType.TransformationLock,
                0,
                AfterRevertDuration);
        }

        // Evolve: อยู่บนสนามครบกำหนดแล้วเปลี่ยนเป็นร่างใหม่ (Silence ปิดความสามารถนี้)
        private static void TryEvolve(BattleManager battle, CardInstance card)
        {
            CardData data = card.Data;

            if (data == null || data.EvolvesInto == null)
                return;

            if (card.IsSilenced || card.Zone != CardZone.Field)
                return;

            if (card.TurnsOnField < data.EvolveAfterTurns)
                return;

            string from = data.CardName;

            card.EvolveTo(data.EvolvesInto);

            battle.Log(from + " evolves into " + card.Data.CardName);
        }
    }
}
