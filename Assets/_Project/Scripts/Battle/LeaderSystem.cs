using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Core;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// ระบบ Leader ของการต่อสู้: สะสม Power จากเหตุการณ์ และมอบการ์ดไม้ตายเมื่อ Power ครบ
    /// ผูกกับ EventBus ของ BattleManager (สร้างตอนเริ่มแมตช์)
    ///
    /// กติกา Power (ทุก Leader ใช้ชุดเดียวกันตอนนี้):
    ///  +1 เมื่อ Leader ของเราโดนความเสียหาย (รวมที่จ่ายเองจากสกิล)
    ///  +1 เมื่อยูนิตของเราตาย
    ///  +1 เมื่อใช้สกิล
    /// ครบ PowerMax แล้วการ์ดไม้ตายเข้ามือ 1 ครั้ง ถ้ามือเต็มจะรอจนมีที่ว่างตอนต้นเทิร์น
    /// </summary>
    public class LeaderSystem
    {
        public const int KaiokenAttack = 2;

        private readonly BattleManager battle;

        public LeaderSystem(BattleManager battle)
        {
            this.battle = battle;

            battle.Events.Raised += Handle;
        }

        public void Setup(BattlePlayer player, LeaderData leader)
        {
            if (player == null)
                return;

            player.Leader = leader;
            player.LeaderPower = 0;
            player.LeaderSkillUsed = false;
            player.LeaderFinisherGiven = false;
        }

        private void Handle(GameEvent e)
        {
            if (e == null)
                return;

            switch (e.Trigger)
            {
                case TriggerType.DamageTaken:
                    // ความเสียหายต่อ Leader: Card เป็น null และ Player คือเจ้าของ Leader
                    if (e.Card == null && e.Player != null)
                        AddPower(e.Player, 1);
                    break;

                case TriggerType.Death:
                    if (e.Card != null && e.Player != null)
                        AddPower(e.Player, 1);
                    break;

                case TriggerType.TurnStart:
                    if (e.Player != null)
                    {
                        e.Player.LeaderSkillUsed = false;

                        TryGrantFinisher(e.Player);
                    }
                    break;
            }
        }

        public void AddPower(BattlePlayer player, int amount)
        {
            if (player == null || player.Leader == null || amount <= 0)
                return;

            player.LeaderPower = Mathf.Min(player.Leader.PowerMax, player.LeaderPower + amount);

            TryGrantFinisher(player);

            battle.Touch();
        }

        public void TryGrantFinisher(BattlePlayer player)
        {
            if (player == null || player.Leader == null ||
                player.LeaderFinisherGiven ||
                player.LeaderPower < player.Leader.PowerMax)
                return;

            // มือเต็ม: รอก่อน (ลองใหม่ตอนต้นเทิร์นถัดไป)
            if (player.Hand.IsFull)
                return;

            CardData data = StarterCardLibrary.Get(player.Leader.FinisherId);

            if (data == null)
                return;

            player.AddToZone(new CardInstance(data), CardZone.Hand);

            player.LeaderFinisherGiven = true;

            battle.Log(player.Name + "'s " + player.Leader.PowerName + " is full: " +
                       data.CardName + " is added to the hand!");

            battle.Touch();
        }
    }

    /// <summary>
    /// กดสกิลของ Leader (เทิร์นละครั้ง) ตอนนี้: Kaioken ของ Goku
    /// จ่าย Energy + เลือดของ Leader แล้วยูนิตทุกตัวของเรา +2 Attack จนจบเทิร์น
    /// </summary>
    public class LeaderSkillCommand : IBattleCommand
    {
        public BattlePlayer Player;

        public LeaderSkillCommand(BattlePlayer player)
        {
            Player = player;
        }

        public static ActionResult Validate(BattleManager battle, BattlePlayer player)
        {
            if (battle == null || player == null || player.Leader == null)
                return ActionResult.Fail("No Leader skill");

            if (battle.IsOver)
                return ActionResult.Fail("The battle is over");

            if (battle.HasPendingChoice)
                return ActionResult.Fail("Make your choice first");

            if (battle.CurrentPlayer != player)
                return ActionResult.Fail("It is not your turn");

            LeaderData leader = player.Leader;

            if (player.LeaderSkillUsed)
                return ActionResult.Fail(leader.SkillName + " was already used this turn");

            if (player.CurrentEnergy < leader.SkillEnergyCost)
                return ActionResult.Fail("Not enough Energy for " + leader.SkillName);

            if (player.LeaderHealth <= leader.SkillHealthCost)
                return ActionResult.Fail("Not enough Leader Health for " + leader.SkillName);

            if (player.Field.Count == 0)
                return ActionResult.Fail("You have no units to empower");

            return ActionResult.Ok();
        }

        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check = Validate(battle, Player);

            if (!check.Success)
                return check;

            LeaderData leader = Player.Leader;

            Player.CurrentEnergy -= leader.SkillEnergyCost;
            Player.LeaderSkillUsed = true;

            battle.Log(Player.Name + " uses " + leader.SkillName + "!");

            // จ่ายเลือดเป็นความเสียหายจริง (ไม่ผ่านเกราะ/ลดดาเมจ)
            battle.DamageLeader(null, Player, leader.SkillHealthCost, true);

            foreach (CardInstance unit in new List<CardInstance>(Player.Field))
                unit.AddStatus(StatusType.AttackUp, LeaderSystem.KaiokenAttack, 1);

            if (battle.Leaders != null)
                battle.Leaders.AddPower(Player, 1);

            return ActionResult.Ok();
        }
    }
}
