using System.Collections.Generic;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// กติกาการต่อสู้ทั้งหมดที่เป็นการ "ตรวจสอบ" (ไม่แก้สถานะเกม)
    /// UI, AI และ Command ใช้ชุดเดียวกัน เพื่อให้กติกาตรงกันทุกที่
    /// </summary>
    public static class BattleRules
    {
        // ---------- Candidates ----------

        /// <summary>
        /// รวมเป้าหมายที่เป็นไปได้ทั้งหมดในสนาม: Leader ทั้งสองฝั่ง + ยูนิตบนสนาม
        /// </summary>
        public static List<TargetCandidate> BuildCandidates(
            BattleManager battle)
        {
            List<TargetCandidate> list = new List<TargetCandidate>();

            if (battle == null)
                return list;

            BattlePlayer[] players = { battle.Player1, battle.Player2 };

            foreach (BattlePlayer player in players)
            {
                list.Add(new TargetCandidate(
                    player.PlayerID, true, false, player));
            }

            foreach (BattlePlayer player in players)
            {
                foreach (CardInstance card in player.Field)
                {
                    list.Add(new TargetCandidate(
                        player.PlayerID, false, true, card));
                }
            }

            return list;
        }

        // ---------- Play ----------

        /// <summary>
        /// เป้าหมายที่ผู้เล่นต้องเลือกเมื่อลงการ์ดใบนี้
        /// ว่าง = ไม่ต้องเลือก (หรือไม่มีเป้าหมายให้เลือก)
        /// </summary>
        public static List<TargetCandidate> GetPlayTargets(
            BattleManager battle,
            BattlePlayer player,
            CardInstance card)
        {
            List<TargetCandidate> empty = new List<TargetCandidate>();

            if (battle == null || player == null ||
                card == null || card.Data == null ||
                card.Data.Actions == null)
                return empty;

            foreach (CardActionData action in card.Data.Actions)
            {
                if (action == null)
                    continue;

                if (action.Trigger != TriggerType.Play)
                    continue;

                if (action.Selection != TargetSelection.Chosen)
                    continue;

                if (action.Target == TargetRule.Self ||
                    action.Target == TargetRule.None)
                    continue;

                List<TargetCandidate> matched =
                    TargetResolver.Resolve(
                        action.Target,
                        player.PlayerID,
                        BuildCandidates(battle));

                return TargetResolver.RemoveHiddenTargets(
                    matched,
                    player.PlayerID);
            }

            return empty;
        }

        /// <summary>
        /// ตรวจเงื่อนไขพื้นฐานของการลงการ์ด (ไม่รวมการเลือกเป้าหมาย)
        /// </summary>
        public static ActionResult ValidateBasicPlay(
            BattleManager battle,
            BattlePlayer player,
            CardInstance card)
        {
            if (battle == null || player == null || card == null)
                return ActionResult.Fail("Invalid play");

            if (battle.IsOver)
                return ActionResult.Fail("The battle is over");

            if (battle.HasPendingChoice)
                return ActionResult.Fail("Make your choice first");

            if (battle.CurrentPlayer != player)
                return ActionResult.Fail("It is not your turn");

            if (card.Zone != CardZone.Hand || !player.Hand.Contains(card))
                return ActionResult.Fail("That card is not in your hand");

            if (card.Data.Type == CardType.Leader)
                return ActionResult.Fail("Leader cards cannot be played");

            if (card.CurrentCost > player.CurrentEnergy)
                return ActionResult.Fail("Not enough energy");

            if (card.Data.Type == CardType.Unit &&
                player.Field.Count >= BattlePlayer.MaxFieldSize)
                return ActionResult.Fail("Your field is full");

            return ActionResult.Ok();
        }

        public static ActionResult ValidatePlay(
            BattleManager battle,
            BattlePlayer player,
            CardInstance card,
            TargetCandidate chosen)
        {
            ActionResult basic = ValidateBasicPlay(battle, player, card);

            if (!basic.Success)
                return basic;

            List<TargetCandidate> options =
                GetPlayTargets(battle, player, card);

            if (options.Count == 0)
                return ActionResult.Ok();

            if (chosen == null)
                return ActionResult.Fail("Choose a target");

            foreach (TargetCandidate option in options)
            {
                if (option.TargetObject == chosen.TargetObject)
                    return ActionResult.Ok();
            }

            return ActionResult.Fail("Invalid target");
        }

        // ---------- Attack ----------

        private static int MaxAttacks(CardInstance card)
        {
            return card.HasKeyword(Keyword.DoubleStrike) ? 2 : 1;
        }

        private static bool IsSummoningSick(
            BattleManager battle,
            CardInstance card)
        {
            return card.SummonedOnTurn == battle.TurnManager.TotalTurns;
        }

        /// <summary>
        /// ยูนิตตัวนี้ "พร้อมโจมตี" หรือไม่ (ยังไม่ดูว่ามีเป้าหมายไหม)
        /// </summary>
        public static ActionResult ValidateCanAttack(
            BattleManager battle,
            BattlePlayer player,
            CardInstance attacker)
        {
            if (battle == null || player == null || attacker == null)
                return ActionResult.Fail("Invalid attack");

            if (battle.IsOver)
                return ActionResult.Fail("The battle is over");

            if (battle.HasPendingChoice)
                return ActionResult.Fail("Make your choice first");

            if (battle.CurrentPlayer != player)
                return ActionResult.Fail("It is not your turn");

            if (attacker.Zone != CardZone.Field ||
                attacker.OwnerID != player.PlayerID ||
                !player.Field.Contains(attacker))
                return ActionResult.Fail("That unit is not on your field");

            if (attacker.IsStunned)
                return ActionResult.Fail("That unit is stunned");

            if (attacker.IsFrozen)
                return ActionResult.Fail("That unit is frozen");

            if (attacker.IsDisarmed)
                return ActionResult.Fail("That unit is disarmed");

            if (attacker.CurrentAttack <= 0)
                return ActionResult.Fail("That unit has no attack");

            if (attacker.AttacksThisTurn >= MaxAttacks(attacker))
                return ActionResult.Fail("That unit already attacked");

            if (IsSummoningSick(battle, attacker) &&
                !attacker.HasKeyword(Keyword.Charge) &&
                !attacker.HasKeyword(Keyword.Rush))
                return ActionResult.Fail("That unit was just summoned");

            return ActionResult.Ok();
        }

        /// <summary>
        /// เป้าหมายที่ยูนิตตัวนี้โจมตีได้ตามกติกา (Taunt, Guard, Stealth, Rush)
        /// </summary>
        public static List<TargetCandidate> GetAttackTargets(
            BattleManager battle,
            BattlePlayer player,
            CardInstance attacker)
        {
            List<TargetCandidate> targets = new List<TargetCandidate>();

            if (battle == null || player == null || attacker == null)
                return targets;

            BattlePlayer enemy = battle.GetOpponent(player);

            if (enemy == null)
                return targets;

            bool unitsOnly =
                IsSummoningSick(battle, attacker) &&
                !attacker.HasKeyword(Keyword.Charge) &&
                attacker.HasKeyword(Keyword.Rush);

            bool hasTaunt = enemy.HasTaunt();
            bool hasGuard = enemy.HasGuard();

            foreach (CardInstance unit in enemy.Field)
            {
                if (unit.IsHidden)
                    continue;

                if (hasTaunt && !unit.HasKeyword(Keyword.Taunt))
                    continue;

                targets.Add(new TargetCandidate(
                    enemy.PlayerID, false, true, unit));
            }

            // Guard: ขณะมี Guard อยู่ โจมตี Leader ไม่ได้
            if (!hasTaunt && !hasGuard && !unitsOnly)
            {
                targets.Add(new TargetCandidate(
                    enemy.PlayerID, true, false, enemy));
            }

            return targets;
        }

        /// <summary>
        /// ยูนิตพร้อมโจมตีและมีเป้าหมายอย่างน้อย 1 อย่าง
        /// </summary>
        public static bool CanAttack(
            BattleManager battle,
            BattlePlayer player,
            CardInstance attacker)
        {
            if (!ValidateCanAttack(battle, player, attacker).Success)
                return false;

            return GetAttackTargets(battle, player, attacker).Count > 0;
        }

        public static ActionResult ValidateAttack(
            BattleManager battle,
            BattlePlayer player,
            CardInstance attacker,
            CardInstance targetUnit,
            BattlePlayer targetLeader)
        {
            ActionResult ready =
                ValidateCanAttack(battle, player, attacker);

            if (!ready.Success)
                return ready;

            if (targetUnit == null && targetLeader == null)
                return ActionResult.Fail("Choose a target");

            List<TargetCandidate> legal =
                GetAttackTargets(battle, player, attacker);

            foreach (TargetCandidate candidate in legal)
            {
                if (targetUnit != null &&
                    candidate.TargetObject == targetUnit)
                    return ActionResult.Ok();

                if (targetUnit == null &&
                    candidate.IsLeader &&
                    candidate.TargetObject == targetLeader)
                    return ActionResult.Ok();
            }

            return ActionResult.Fail("That target cannot be attacked");
        }
    }
}
