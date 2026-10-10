using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// AI แบบง่าย: คืน Command ถัดไปที่ควรทำ (null = จบเทิร์น)
    /// ไม่แก้สถานะเกมเอง ให้ผู้เรียกส่ง Command ผ่าน BattleManager.Submit
    ///
    /// ลำดับความคิด: ลงการ์ดที่แพงที่สุดที่จ่ายไหวก่อน แล้วค่อยโจมตี
    /// </summary>
    public static class AIPlanner
    {
        public static IBattleCommand NextCommand(
            BattleManager battle,
            BattlePlayer player)
        {
            if (battle == null || player == null)
                return null;

            if (battle.IsOver || battle.CurrentPlayer != player)
                return null;

            IBattleCommand play = PlanPlay(battle, player);

            if (play != null)
                return play;

            return PlanAttack(battle, player);
        }

        // ---------- ลงการ์ด ----------

        private static IBattleCommand PlanPlay(
            BattleManager battle,
            BattlePlayer player)
        {
            List<CardInstance> hand =
                new List<CardInstance>(player.Hand.Cards);

            // เรียงจากราคามากไปน้อย
            hand.Sort((a, b) => b.CurrentCost.CompareTo(a.CurrentCost));

            foreach (CardInstance card in hand)
            {
                if (!BattleRules.ValidateBasicPlay(battle, player, card).Success)
                    continue;

                List<TargetCandidate> options =
                    BattleRules.GetPlayTargets(battle, player, card);

                TargetCandidate chosen = null;

                if (options.Count > 0)
                {
                    chosen = PickPlayTarget(card, player, options);

                    if (chosen == null)
                        continue;
                }

                if (BattleRules.ValidatePlay(battle, player, card, chosen).Success)
                    return new PlayCardCommand(player, card, chosen);
            }

            return null;
        }

        private static TargetCandidate PickPlayTarget(
            CardInstance card,
            BattlePlayer self,
            List<TargetCandidate> options)
        {
            // ถ้าเป็นเป้าหมายฝั่งเรา (บัฟ/รักษา) เลือกตัวที่ตีแรงสุด
            // ถ้าเป็นฝั่งศัตรู เลือกยูนิตที่ตีแรงสุด
            TargetCandidate best = null;
            int bestScore = int.MinValue;

            foreach (TargetCandidate option in options)
            {
                int score;

                CardInstance unit = option.TargetObject as CardInstance;

                // ไม่เลือกเป้าที่ผลของการ์ดไม่มีความหมาย (เช่น Stun ตัวที่ติด Stun อยู่แล้ว)
                if (unit != null && IsRedundantTarget(card, unit))
                    continue;

                if (unit != null)
                    score = unit.CurrentAttack * 2 + unit.CurrentHealth;
                else
                    score = 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = option;
                }
            }

            return best;
        }

        // การ์ดนี้ใช้กับยูนิตตัวนี้แล้วไม่เกิดประโยชน์หรือไม่
        // (เฉพาะการ์ดที่มีแต่ผลสถานะ/Keyword ถ้ามีดาเมจหรือผลอื่นปนอยู่ถือว่ามีประโยชน์)
        private static bool IsRedundantTarget(CardInstance card, CardInstance unit)
        {
            if (card == null || card.Data == null || card.Data.Actions == null)
                return false;

            foreach (CardActionData action in card.Data.Actions)
            {
                if (action == null ||
                    action.Trigger != TriggerType.Play ||
                    action.Selection != TargetSelection.Chosen)
                    continue;

                if (action.Effects == null || action.Effects.Count == 0)
                    return false;

                foreach (EffectData effect in action.Effects)
                {
                    if (!IsRedundantEffect(effect, unit))
                        return false;
                }

                return true;
            }

            return false;
        }

        private static bool IsRedundantEffect(EffectData effect, CardInstance unit)
        {
            switch (effect.Type)
            {
                case EffectType.Stun:
                    return unit.IsStunned ||
                        ResistsStatus(unit, StatusType.Stun);

                case EffectType.Silence:
                    return unit.IsSilenced ||
                        ResistsStatus(unit, StatusType.Silence);

                case EffectType.Burn:
                    return ResistsStatus(unit, StatusType.Burn);

                case EffectType.ApplyStatus:
                    if (ResistsStatus(unit, effect.Status))
                        return true;

                    // สถานะที่ซ้อนค่าไม่ได้ และติดอยู่แล้ว
                    return !StatusInfo.Stacks(effect.Status) &&
                        effect.Status != StatusType.Burn &&
                        unit.HasStatus(effect.Status);

                case EffectType.AddKeyword:
                    return (unit.Keywords & effect.Keyword) == effect.Keyword;

                case EffectType.RemoveKeyword:
                    return (unit.Keywords & effect.Keyword) == Keyword.None;

                default:
                    return false;
            }
        }

        // สถานะเสียที่ยูนิตนี้กันได้ (Immune / Protected / Unstoppable)
        private static bool ResistsStatus(CardInstance unit, StatusType type)
        {
            if (!StatusInfo.IsHarmful(type))
                return false;

            if (unit.HasKeyword(Keyword.Immune) || unit.HasStatus(StatusType.Protected))
                return true;

            return unit.HasKeyword(Keyword.Unstoppable) && StatusInfo.StopsAttack(type);
        }

        // ---------- โจมตี ----------

        private static IBattleCommand PlanAttack(
            BattleManager battle,
            BattlePlayer player)
        {
            foreach (CardInstance attacker in
                new List<CardInstance>(player.Field))
            {
                if (!BattleRules.CanAttack(battle, player, attacker))
                    continue;

                List<TargetCandidate> targets =
                    BattleRules.GetAttackTargets(battle, player, attacker);

                TargetCandidate pick = PickAttackTarget(attacker, targets);

                if (pick == null)
                    continue;

                CardInstance targetUnit = pick.TargetObject as CardInstance;
                BattlePlayer targetLeader = pick.TargetObject as BattlePlayer;

                return new AttackCommand(
                    player, attacker, targetUnit, targetLeader);
            }

            return null;
        }

        private static TargetCandidate PickAttackTarget(
            CardInstance attacker,
            List<TargetCandidate> targets)
        {
            TargetCandidate leader = null;
            TargetCandidate bestTrade = null;
            int bestTradeScore = int.MinValue;
            TargetCandidate weakest = null;
            int weakestHealth = int.MaxValue;

            foreach (TargetCandidate candidate in targets)
            {
                BattlePlayer leaderPlayer =
                    candidate.TargetObject as BattlePlayer;

                if (leaderPlayer != null)
                {
                    // ตีจบเกมได้ ตีเลย
                    if (attacker.CurrentAttack >= leaderPlayer.LeaderHealth)
                        return candidate;

                    leader = candidate;
                    continue;
                }

                CardInstance unit = candidate.TargetObject as CardInstance;

                if (unit == null)
                    continue;

                bool killsTarget =
                    attacker.CurrentAttack >= unit.CurrentHealth ||
                    attacker.HasKeyword(Keyword.Poison);

                // ความเสียหายที่ตัวเองจะโดนสวนกลับจริง
                int counter =
                    attacker.HasKeyword(Keyword.Stealth) ||
                    !AttackCommand.CanCounter(unit)
                        ? 0
                        : System.Math.Max(0, unit.CurrentAttack - attacker.ImmuneAmount);

                bool survives =
                    counter < attacker.CurrentHealth ||
                    attacker.HasKeyword(Keyword.Shield) ||
                    (attacker.HasKeyword(Keyword.QuickAttack) &&
                     !unit.HasKeyword(Keyword.QuickAttack) &&
                     killsTarget);

                // แลกที่ได้เปรียบ: ฆ่าได้และตัวเองรอด หรือฆ่าตัวที่แพงกว่า
                if (killsTarget && (survives || unit.CurrentCost > attacker.CurrentCost))
                {
                    int score = unit.CurrentCost * 10 + unit.CurrentAttack;

                    if (survives)
                        score += 100;

                    if (score > bestTradeScore)
                    {
                        bestTradeScore = score;
                        bestTrade = candidate;
                    }
                }

                if (unit.CurrentHealth < weakestHealth)
                {
                    weakestHealth = unit.CurrentHealth;
                    weakest = candidate;
                }
            }

            if (bestTrade != null)
                return bestTrade;

            if (leader != null)
                return leader;

            // ติด Taunt: ต้องตีตัวที่อ่อนที่สุด
            return weakest;
        }
    }
}
