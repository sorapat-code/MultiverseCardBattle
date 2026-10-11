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
    /// Leader แต่ละตัวมี PowerGain (LeaderData) บอกว่าเหตุการณ์ไหนให้ +1:
    ///  LeaderDamaged / UnitDied / UnitPlayed / SpellPlayed / EnemyUnitDied / SkillUsed
    /// ครบ PowerMax แล้วการ์ดไม้ตายเข้ามือ 1 ครั้ง ถ้ามือเต็มจะรอจนมีที่ว่างตอนต้นเทิร์น
    /// </summary>
    public class LeaderSystem
    {
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
                        Gain(e.Player, PowerGain.LeaderDamaged);
                    break;

                case TriggerType.Death:
                    if (e.Card != null && e.Player != null)
                    {
                        Gain(e.Player, PowerGain.UnitDied);
                        Gain(battle.GetOpponent(e.Player), PowerGain.EnemyUnitDied);
                    }
                    break;

                case TriggerType.Play:
                    // ลงยูนิตจากมือ (ยูนิตที่ Summon ด้วยเอฟเฟกต์ไม่ผ่านจุดนี้)
                    if (e.Card != null && e.Player != null && e.Card.Data != null &&
                        e.Card.Data.Type == CardType.Unit)
                        Gain(e.Player, PowerGain.UnitPlayed);
                    break;

                case TriggerType.Spellburst:
                    // ยิงหนึ่งครั้งต่อ Spell ที่ใช้
                    if (e.Player != null)
                        Gain(e.Player, PowerGain.SpellPlayed);
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

        /// <summary>Power +1 ถ้า Leader ของผู้เล่นนับเหตุการณ์ชนิดนี้</summary>
        public void Gain(BattlePlayer player, PowerGain reason)
        {
            if (player == null || player.Leader == null)
                return;

            if ((player.Leader.PowerGain & reason) == 0)
                return;

            AddPower(player, 1);
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
    /// กดสกิลของ Leader (เทิร์นละครั้ง) การทำงานแยกตาม LeaderData.SkillKind
    /// ใช้ได้ทั้งผู้เล่นและบอสศัตรู (Player = ผู้ใช้สกิล, ศัตรู = battle.GetOpponent(Player))
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

            BattlePlayer enemy = battle.GetOpponent(player);

            switch (leader.SkillKind)
            {
                case LeaderSkillKind.BuffAttack:
                case LeaderSkillKind.Barrier:
                    if (player.Field.Count == 0)
                        return ActionResult.Fail("You have no units to empower");
                    break;

                case LeaderSkillKind.Heal:
                    if (player.LeaderHealth >= player.MaxLeaderHealth && player.Field.Count == 0)
                        return ActionResult.Fail("Nothing to heal");
                    break;

                case LeaderSkillKind.SummonTokens:
                    if (player.Field.Count >= BattlePlayer.MaxFieldSize)
                        return ActionResult.Fail("Your field is full");
                    break;

                case LeaderSkillKind.DamageEnemies:
                    if (enemy.Field.Count == 0 && !leader.SkillHitsLeader)
                        return ActionResult.Fail("No enemy units to hit");
                    break;

                case LeaderSkillKind.WeakenEnemies:
                    if (enemy.Field.Count == 0)
                        return ActionResult.Fail("No enemy units to weaken");
                    break;
            }

            return ActionResult.Ok();
        }

        public ActionResult Execute(BattleManager battle)
        {
            ActionResult check = Validate(battle, Player);

            if (!check.Success)
                return check;

            LeaderData leader = Player.Leader;
            BattlePlayer enemy = battle.GetOpponent(Player);

            Player.CurrentEnergy -= leader.SkillEnergyCost;
            Player.LeaderSkillUsed = true;

            battle.Log(Player.Name + " uses " + leader.SkillName + "!");

            // จ่ายเลือดเป็นความเสียหายจริง (ไม่ผ่านเกราะ/ลดดาเมจ)
            if (leader.SkillHealthCost > 0)
                battle.DamageLeader(null, Player, leader.SkillHealthCost, true);

            switch (leader.SkillKind)
            {
                case LeaderSkillKind.BuffAttack:
                    foreach (CardInstance unit in new List<CardInstance>(Player.Field))
                        unit.AddStatus(StatusType.AttackUp, leader.SkillValue, 1);
                    break;

                case LeaderSkillKind.Barrier:
                    foreach (CardInstance unit in new List<CardInstance>(Player.Field))
                        unit.AddStatus(StatusType.Barrier, leader.SkillValue, 2);
                    break;

                case LeaderSkillKind.Heal:
                    battle.HealLeader(null, Player, leader.SkillValue);

                    foreach (CardInstance unit in new List<CardInstance>(Player.Field))
                        battle.HealUnit(null, unit, leader.SkillValue);
                    break;

                case LeaderSkillKind.SummonTokens:
                    CardData tokenData = StarterCardLibrary.Get(leader.SkillTokenId);

                    for (int i = 0; i < leader.SkillCount && tokenData != null; i++)
                    {
                        CardInstance token = new CardInstance(tokenData);

                        token.OwnerID = Player.PlayerID;

                        // สนามเต็มกลางทาง: ไม่เรียกตัวที่เหลือ
                        if (!battle.SummonUnit(token, Player))
                            break;
                    }
                    break;

                case LeaderSkillKind.DamageEnemies:
                    foreach (CardInstance unit in new List<CardInstance>(enemy.Field))
                        battle.DamageUnit(null, unit, leader.SkillValue);

                    if (leader.SkillHitsLeader)
                        battle.DamageLeader(null, enemy, leader.SkillValue);
                    break;

                case LeaderSkillKind.WeakenEnemies:
                    foreach (CardInstance unit in new List<CardInstance>(enemy.Field))
                        unit.AddStatus(StatusType.Weakness, 1, 1);
                    break;
            }

            if (battle.Leaders != null)
                battle.Leaders.Gain(Player, PowerGain.SkillUsed);

            return ActionResult.Ok();
        }
    }
}
