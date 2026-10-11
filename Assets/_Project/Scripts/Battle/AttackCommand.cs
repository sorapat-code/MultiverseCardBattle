using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Battle
{
    public class AttackCommand : IBattleCommand
    {
        public BattlePlayer Player;
        public CardInstance Attacker;

        // ตี Unit: ใส่ TargetUnit / ตี Leader: ใส่ TargetLeader
        public CardInstance TargetUnit;
        public BattlePlayer TargetLeader;

        public AttackCommand(
            BattlePlayer player,
            CardInstance attacker,
            CardInstance targetUnit,
            BattlePlayer targetLeader)
        {
            Player = player;
            Attacker = attacker;
            TargetUnit = targetUnit;
            TargetLeader = targetLeader;
        }

        /// <summary>
        /// ประกาศโจมตี: วางลงเชนให้คู่ต่อสู้ตอบโต้ได้ แล้วค่อยโจมตีจริงเมื่อถึงคิว
        /// </summary>
        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check =
                BattleRules.ValidateAttack(
                    battle, Player, Attacker, TargetUnit, TargetLeader);

            if (!check.Success)
                return check;

            battle.DeclareToChain(ChainLink.ForAttack(this));

            return ActionResult.Ok();
        }

        /// <summary>โจมตีจริง (เรียกเมื่อถึงคิวบนเชน) ตรวจกติกาซ้ำเพราะสนามอาจเปลี่ยนไปแล้ว</summary>
        public void ResolveAttack(BattleManager battle)
        {
            ActionResult check =
                BattleRules.ValidateAttack(
                    battle, Player, Attacker, TargetUnit, TargetLeader);

            if (!check.Success)
            {
                battle.Log(Attacker.Data.CardName + "'s attack fizzles");

                return;
            }

            CardInstance targetUnit = TargetUnit;
            BattlePlayer targetLeader = TargetLeader;

            // Confusion: เป้าหมายถูกสุ่มใหม่ (อาจเป็นพวกเดียวกัน)
            if (Attacker.IsConfused)
                Redirect(battle, ref targetUnit, ref targetLeader);

            Attacker.AttacksThisTurn++;
            Attacker.TotalAttacks++;

            if (targetUnit != null && targetUnit.OwnerID != Player.PlayerID)
                Attacker.UnitAttacksThisTurn++;

            // ซุ่มโจมตี: ตีจาก Stealth ครั้งแรกจะไม่โดนสวนกลับ
            bool ambush = Attacker.HasKeyword(Keyword.Stealth);

            // โจมตีแล้วหลุด Stealth
            Attacker.RemoveKeyword(Keyword.Stealth);

            // ยูนิตที่ถูกเล็งโดยการโจมตี (OnTarget) อาจตอบสนองก่อนการต่อสู้
            if (targetUnit != null)
            {
                battle.Publish(
                    TriggerType.OnTarget,
                    card: targetUnit,
                    player: battle.GetPlayer(targetUnit.OwnerID),
                    target: Attacker,
                    targetPlayer: Player,
                    byAttack: true);

                if (Attacker.Zone != CardZone.Field ||
                    targetUnit.Zone != CardZone.Field)
                    return;
            }

            int attackerHealthBefore = Attacker.CurrentHealth;
            int targetHealthBefore = targetUnit != null ? targetUnit.CurrentHealth : 0;

            if (targetUnit != null)
                AttackUnit(battle, targetUnit, ambush);
            else
                AttackLeader(battle, targetLeader);

            if (targetUnit != null)
            {
                AfterUnitCombat(
                    battle, targetUnit, attackerHealthBefore, targetHealthBefore);
            }

            // หลังโจมตี: ความสามารถแบบ "หลังโจมตี" (เช่น ได้ Shield)
            if (Attacker.Zone == CardZone.Field && Attacker.CurrentHealth > 0)
            {
                battle.Publish(
                    TriggerType.AfterAttack,
                    card: Attacker,
                    player: Player,
                    target: targetUnit,
                    targetPlayer: targetLeader);
            }

            // Trident: ยูนิตที่สวมโจมตีเสร็จแล้วกลับขึ้นมือพร้อมอุปกรณ์ (ถ้ายังรอดอยู่)
            battle.ReturnGearedUnit(Attacker);

            // ยูนิตเดียวกันโจมตีครบ 3 ครั้งโดยไม่ตาย (ปลดล็อก Vergil)
            if (Attacker.Zone == CardZone.Field && Attacker.CurrentHealth > 0)
                battle.Stats.NoteSurvivingAttacks(Player.PlayerID, Attacker.TotalAttacks);
        }

        // หลังต่อสู้ระหว่างยูนิต: Abysswalker (ATK +1 จนจบเทิร์น) และนับชัยชนะสำหรับปลดล็อกการ์ดลับ
        private void AfterUnitCombat(
            BattleManager battle,
            CardInstance targetUnit,
            int attackerHealthBefore,
            int targetHealthBefore)
        {
            if (Attacker.Zone == CardZone.Field &&
                Attacker.CurrentHealth < attackerHealthBefore &&
                Attacker.HasPassive(CardPassive.Abysswalker))
            {
                Attacker.TurnAttackBonus++;

                battle.Log(Attacker.Data.CardName + " grows stronger (+1 Attack)");
            }

            if (targetUnit.Zone == CardZone.Field &&
                targetUnit.CurrentHealth < targetHealthBefore &&
                targetUnit.HasPassive(CardPassive.Abysswalker))
            {
                targetUnit.TurnAttackBonus++;

                battle.Log(targetUnit.Data.CardName + " grows stronger (+1 Attack)");
            }

            bool defenderDown =
                targetUnit.CurrentHealth <= 0 || targetUnit.Zone != CardZone.Field;

            if (defenderDown &&
                Attacker.CurrentHealth > 0 &&
                targetUnit.OwnerID != Player.PlayerID)
            {
                battle.Stats.AddCombatWin(Player.PlayerID);
            }
        }

        private void Redirect(
            BattleManager battle,
            ref CardInstance targetUnit,
            ref BattlePlayer targetLeader)
        {
            List<TargetCandidate> pool =
                BattleRules.GetAttackTargets(battle, Player, Attacker);

            foreach (CardInstance ally in Player.Field)
            {
                if (ally != Attacker)
                {
                    pool.Add(new TargetCandidate(
                        Player.PlayerID, false, true, ally));
                }
            }

            if (pool.Count == 0)
                return;

            TargetCandidate pick = pool[Random.Range(0, pool.Count)];

            battle.Log(Attacker.Data.CardName + " is confused");

            targetUnit = pick.TargetObject as CardInstance;
            targetLeader = pick.TargetObject as BattlePlayer;
        }

        // ยูนิตที่ถูกควบคุม (Stun / Freeze / Disarm) สวนกลับไม่ได้
        public static bool CanCounter(CardInstance defender)
        {
            return defender != null &&
                !defender.IsStunned &&
                !defender.IsFrozen &&
                !defender.IsDisarmed;
        }

        private void AttackUnit(
            BattleManager battle,
            CardInstance targetUnit,
            bool ambush)
        {
            BattlePlayer targetOwner = battle.GetPlayer(targetUnit.OwnerID);

            battle.Log(
                Attacker.Data.CardName + " attacks " +
                targetUnit.Data.CardName);

            battle.Publish(
                TriggerType.Attack,
                card: Attacker,
                player: Player,
                target: targetUnit,
                targetPlayer: targetOwner);

            // Trigger อาจทำให้ใครตายหรือถูกย้ายไปแล้ว
            if (Attacker.Zone != CardZone.Field ||
                targetUnit.Zone != CardZone.Field)
                return;

            // ผู้ช่วย (เช่น Mini Herta) ยิงใส่เป้าหมายก่อนการต่อสู้ ไม่นับเป็นการโจมตี
            battle.AssistStrikes(Attacker, targetUnit);

            if (Attacker.Zone != CardZone.Field ||
                targetUnit.Zone != CardZone.Field)
                return;

            // เป้าหมายตายไปแล้วจาก Trigger ก่อนโจมตี (เช่น Judgment of the End) ไม่ต้องสู้ต่อ
            if (targetUnit.CurrentHealth <= 0)
                return;

            int attackerDamage = Attacker.CurrentAttack;
            // Retaliate: สวนกลับเสมอ แม้ถูกควบคุมอยู่ (ยกเว้นโดนซุ่มโจมตี)
            bool retaliates = targetUnit.HasKeyword(Keyword.Retaliate);

            int counterDamage =
                ambush || (!CanCounter(targetUnit) && !retaliates)
                    ? 0
                    : targetUnit.CurrentAttack;
            int healthBefore = targetUnit.CurrentHealth;

            bool attackerQuick = Attacker.HasKeyword(Keyword.QuickAttack);
            bool defenderQuick = targetUnit.HasKeyword(Keyword.QuickAttack);

            int dealt;

            if (defenderQuick && !attackerQuick)
            {
                // ฝ่ายป้องกันตีก่อน ถ้าผู้โจมตีตายก็ไม่ได้ตีกลับ
                battle.DamageUnit(targetUnit, Attacker, counterDamage);

                dealt = Attacker.CurrentHealth > 0
                    ? battle.DamageUnit(Attacker, targetUnit, attackerDamage)
                    : 0;
            }
            else if (attackerQuick && !defenderQuick)
            {
                // ผู้โจมตีตีก่อน ถ้าเป้าหมายตายก็ไม่โดนตีกลับ
                dealt = battle.DamageUnit(Attacker, targetUnit, attackerDamage);

                // Retaliate: ตายจากการโจมตีนี้แล้วก็ยังสวนกลับ
                if (targetUnit.CurrentHealth > 0 || retaliates)
                    battle.DamageUnit(targetUnit, Attacker, counterDamage);
            }
            else
            {
                dealt = battle.DamageUnit(Attacker, targetUnit, attackerDamage);

                battle.DamageUnit(targetUnit, Attacker, counterDamage);
            }

            // Thorns: สะท้อนความเสียหายกลับ ไม่ว่าเป้าหมายจะตายหรือไม่
            if (dealt > 0 &&
                targetUnit.HasKeyword(Keyword.Thorns) &&
                Attacker.Zone == CardZone.Field &&
                Attacker.CurrentHealth > 0)
            {
                battle.Log(targetUnit.Data.CardName + "'s thorns hurt " + Attacker.Data.CardName);

                battle.DamageUnit(targetUnit, Attacker, BattleManager.ThornsDamage, false);
            }

            int excess = dealt - healthBefore;

            if (excess <= 0)
                return;

            if (Attacker.HasKeyword(Keyword.Pierce))
            {
                // Pierce: ส่วนเกินทะลุไปที่ Leader ของเจ้าของเป้าหมาย
                battle.Log(Attacker.Data.CardName + " pierces for " + excess);

                battle.DamageLeader(Attacker, targetOwner, excess, false);
            }
            else if (Attacker.HasKeyword(Keyword.Overwhelm))
            {
                // Overwhelm: ส่วนเกินกระเด็นไปยูนิตข้างเคียง
                CardInstance neighbor = FindNeighbor(targetOwner, targetUnit);

                if (neighbor != null)
                {
                    battle.Log(Attacker.Data.CardName + " overwhelms for " + excess);

                    battle.DamageUnit(Attacker, neighbor, excess, false);
                }
            }
        }

        // ยูนิตถัดไปทางขวา ถ้าไม่มีให้ใช้ทางซ้าย
        private static CardInstance FindNeighbor(
            BattlePlayer owner,
            CardInstance unit)
        {
            if (owner == null)
                return null;

            int index = owner.Field.IndexOf(unit);

            if (index < 0)
                return null;

            if (index + 1 < owner.Field.Count)
                return owner.Field[index + 1];

            if (index - 1 >= 0)
                return owner.Field[index - 1];

            return null;
        }

        private void AttackLeader(BattleManager battle, BattlePlayer targetLeader)
        {
            BattlePlayer defender = targetLeader != null
                ? targetLeader
                : battle.GetOpponent(Player);

            battle.Log(
                Attacker.Data.CardName + " attacks " + defender.Name);

            battle.Publish(
                TriggerType.Attack,
                card: Attacker,
                player: Player,
                targetPlayer: defender);

            battle.Publish(
                TriggerType.LeaderAttack,
                card: Attacker,
                player: Player,
                targetPlayer: defender);

            if (Attacker.Zone == CardZone.Field)
            {
                battle.DamageLeader(
                    Attacker, defender, Attacker.CurrentAttack, false);
            }
        }
    }
}
