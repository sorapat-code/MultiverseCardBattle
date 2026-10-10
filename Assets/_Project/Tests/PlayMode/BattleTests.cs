using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบกติกาการต่อสู้ผ่าน Command (ไม่ผ่าน UI)
    /// </summary>
    public class BattleTests
    {
        private GameObject host;
        private BattleManager battle;
        private readonly List<CardData> created = new List<CardData>();

        private BattlePlayer P1 { get { return battle.Player1; } }
        private BattlePlayer P2 { get { return battle.Player2; } }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            BattleSetup.Reset();

            CardData filler = MakeData("FILLER", 1, 1, 1, Keyword.None);

            List<CardData> deck = new List<CardData>();

            for (int i = 0; i < 30; i++)
                deck.Add(filler);

            BattleSetup.PlayerDeck = deck;
            BattleSetup.EnemyDeck = new List<CardData>(deck);

            host = new GameObject("TestBattle");
            battle = host.AddComponent<BattleManager>();

            // รอให้ Start ของ BattleManager เริ่มการต่อสู้
            yield return null;

            Assert.IsTrue(battle.HasStarted);
            Assert.AreEqual(P1, battle.CurrentPlayer);

            // เคลียร์สนามให้พร้อมจัดสถานการณ์เอง
            P1.Hand.Clear();
            P2.Hand.Clear();
            P1.CurrentEnergy = 10;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);

            foreach (CardData data in created)
                Object.DestroyImmediate(data);

            created.Clear();

            BattleSetup.Reset();
        }

        private CardData MakeData(
            string id, int cost, int attack, int health, Keyword keywords)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            data.CardID = id;
            data.CardName = id;
            data.Type = CardType.Unit;
            data.Cost = cost;
            data.Attack = attack;
            data.Health = health;
            data.Keywords = keywords;

            created.Add(data);

            return data;
        }

        // วางยูนิตบนสนามโดยตรง และทำให้พร้อมโจมตี (ไม่ติด Summoning Sickness)
        private CardInstance PutOnField(
            BattlePlayer owner, string id, int attack, int health, Keyword keywords)
        {
            CardInstance card =
                new CardInstance(MakeData(id, 1, attack, health, keywords));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        [Test]
        public void PlayUnit_SpendsEnergyAndEntersField()
        {
            CardInstance card =
                new CardInstance(MakeData("U1", 3, 2, 2, Keyword.None));

            P1.Hand.Add(card);

            ActionResult result = battle.Submit(new PlayCardCommand(P1, card));

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(7, P1.CurrentEnergy);
            Assert.Contains(card, P1.Field);
            Assert.IsFalse(P1.Hand.Contains(card));
        }

        [Test]
        public void PlayUnit_FailsWithoutEnergy()
        {
            CardInstance card =
                new CardInstance(MakeData("U2", 5, 2, 2, Keyword.None));

            P1.Hand.Add(card);
            P1.CurrentEnergy = 2;

            ActionResult result = battle.Submit(new PlayCardCommand(P1, card));

            Assert.IsFalse(result.Success);
            Assert.IsTrue(P1.Hand.Contains(card));
        }

        [Test]
        public void NewUnit_CannotAttackOnSummonTurn()
        {
            CardInstance card =
                new CardInstance(MakeData("U3", 1, 2, 2, Keyword.None));

            P1.Hand.Add(card);

            battle.Submit(new PlayCardCommand(P1, card));

            ActionResult attack =
                battle.Submit(new AttackCommand(P1, card, null, P2));

            Assert.IsFalse(attack.Success);
        }

        [Test]
        public void Taunt_BlocksLeaderAttack()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.None);
            CardInstance taunt = PutOnField(P2, "T", 1, 5, Keyword.Taunt);

            ActionResult leaderAttack =
                battle.Submit(new AttackCommand(P1, attacker, null, P2));

            Assert.IsFalse(leaderAttack.Success);

            ActionResult unitAttack =
                battle.Submit(new AttackCommand(P1, attacker, taunt, null));

            Assert.IsTrue(unitAttack.Success, unitAttack.Message);
        }

        [Test]
        public void Attack_TradesAndKilledUnitsGoToGraveyard()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.None);
            CardInstance defender = PutOnField(P2, "D", 3, 3, Keyword.None);

            ActionResult result =
                battle.Submit(new AttackCommand(P1, attacker, defender, null));

            Assert.IsTrue(result.Success, result.Message);
            Assert.Contains(attacker, P1.Graveyard);
            Assert.Contains(defender, P2.Graveyard);
            Assert.IsEmpty(P1.Field);
            Assert.IsEmpty(P2.Field);
        }

        [Test]
        public void Shield_AbsorbsOneHit()
        {
            CardInstance target = PutOnField(P2, "S", 1, 3, Keyword.Shield);

            int first = battle.DamageUnit(null, target, 2);
            int second = battle.DamageUnit(null, target, 2);

            Assert.AreEqual(0, first);
            Assert.AreEqual(2, second);
            Assert.AreEqual(1, target.CurrentHealth);
        }

        [Test]
        public void Poison_KillsAfterAnyDamage()
        {
            CardInstance source = PutOnField(P1, "P", 1, 1, Keyword.Poison);
            CardInstance target = PutOnField(P2, "V", 1, 10, Keyword.None);

            battle.DamageUnit(source, target, 1);
            battle.Resolve();

            Assert.Contains(target, P2.Graveyard);
        }

        [Test]
        public void Lifesteal_HealsOwnerLeader()
        {
            CardInstance source = PutOnField(P1, "L", 3, 3, Keyword.Lifesteal);
            CardInstance target = PutOnField(P2, "V", 1, 10, Keyword.None);

            P1.LeaderHealth = 10;

            battle.DamageUnit(source, target, 3);

            Assert.AreEqual(13, P1.LeaderHealth);
        }

        [Test]
        public void Burn_DamagesUnitAtOwnersTurnStart()
        {
            CardInstance unit = PutOnField(P1, "B", 1, 5, Keyword.None);

            unit.AddStatus(StatusType.Burn, 2, 3);

            StatusProcessor.OnTurnStart(battle, P1);

            Assert.AreEqual(3, unit.CurrentHealth);
        }

        [Test]
        public void Stun_PreventsAttack()
        {
            CardInstance unit = PutOnField(P1, "St", 2, 2, Keyword.None);

            unit.AddStatus(StatusType.Stun, 0, 1);

            ActionResult result =
                battle.Submit(new AttackCommand(P1, unit, null, P2));

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void LethalLeaderDamage_EndsBattle()
        {
            battle.DamageLeader(null, P2, 999);
            battle.Resolve();

            Assert.IsTrue(battle.IsOver);
            Assert.AreEqual(P1, battle.Winner);
        }

        [Test]
        public void EndTurn_PassesTurnToOpponent()
        {
            ActionResult result = battle.Submit(new EndTurnCommand(P1));

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(P2, battle.CurrentPlayer);

            ActionResult wrongPlayer = battle.Submit(new EndTurnCommand(P1));

            Assert.IsFalse(wrongPlayer.Success);
        }

        [Test]
        public void AI_ProducesLegalCommands()
        {
            battle.Submit(new EndTurnCommand(P1));

            P2.CurrentEnergy = 10;

            for (int i = 0; i < 40; i++)
            {
                IBattleCommand command = AIPlanner.NextCommand(battle, P2);

                if (command == null)
                    break;

                ActionResult result = battle.Submit(command);

                Assert.IsTrue(result.Success, "AI produced an illegal command: " + result.Message);
            }
        }
    }
}
