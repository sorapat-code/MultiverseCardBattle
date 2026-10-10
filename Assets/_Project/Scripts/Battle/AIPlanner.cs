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

                bool survives =
                    unit.CurrentAttack < attacker.CurrentHealth ||
                    attacker.HasKeyword(Keyword.Shield);

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
