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
    /// <summary>ทดสอบ Vergil, Devil Trigger และเงื่อนไขปลดล็อก The Power of the Yamato</summary>
    public class VergilTests
    {
        private GameObject host;
        private BattleManager battle;
        private CardData filler;
        private CardData spell;

        private BattlePlayer P1 { get { return battle.Player1; } }
        private BattlePlayer P2 { get { return battle.Player2; } }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            BattleSetup.Reset();

            filler = ScriptableObject.CreateInstance<CardData>();

            filler.CardID = "FILLER";
            filler.CardName = "FILLER";
            filler.Type = CardType.Unit;
            filler.Cost = 1;
            filler.Attack = 1;
            filler.Health = 5;

            spell = ScriptableObject.CreateInstance<CardData>();

            spell.CardID = "TEST_SPELL";
            spell.CardName = "TEST_SPELL";
            spell.Type = CardType.Spell;
            spell.Cost = 3;

            List<CardData> deck = new List<CardData>();

            for (int i = 0; i < 30; i++)
                deck.Add(filler);

            BattleSetup.PlayerDeck = deck;
            BattleSetup.EnemyDeck = new List<CardData>(deck);

            host = new GameObject("TestBattle");
            battle = host.AddComponent<BattleManager>();

            yield return null;

            P1.Hand.Clear();
            P2.Hand.Clear();
            P1.CurrentEnergy = 10;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);
            Object.DestroyImmediate(filler);
            Object.DestroyImmediate(spell);

            BattleSetup.Reset();
        }

        private CardInstance Place(BattlePlayer owner, string id)
        {
            CardInstance card = new CardInstance(StarterCardLibrary.Get(id));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        private CardInstance SpellInHand()
        {
            CardInstance card = new CardInstance(spell);

            card.OwnerID = P1.PlayerID;

            P1.Hand.Add(card);

            return card;
        }

        [Test]
        public void JudgmentCut_DamagesEnemiesAndGivesExtraAttack()
        {
            CardInstance enemy = Place(P2, "SUP_026");

            CardInstance vergil = new CardInstance(
                StarterCardLibrary.Get(StarterCardLibrary.VergilId));

            vergil.OwnerID = P1.PlayerID;
            P1.Hand.Add(vergil);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, vergil)).Success);

            Assert.AreEqual(4, enemy.CurrentHealth);

            // Charge + โจมตีเพิ่ม 1 ครั้ง = 2 ครั้งในเทิร์นนี้
            Assert.IsTrue(battle.Submit(
                new AttackCommand(P1, vergil, enemy, null)).Success);

            Assert.IsTrue(battle.Submit(
                new AttackCommand(P1, vergil, null, P2)).Success);

            Assert.IsFalse(battle.Submit(
                new AttackCommand(P1, vergil, null, P2)).Success);
        }

        [Test]
        public void DevilTrigger_TransformsKeepsDamageAndRevertsAfterTwoTurns()
        {
            CardInstance filler1 = Place(P1, "SUP_001");
            CardInstance vergil = Place(P1, StarterCardLibrary.VergilId);

            int index = P1.Field.IndexOf(vergil);

            vergil.CurrentHealth = 4; // เสียหายสะสม 2

            Assert.IsTrue(DevilTriggerCommand.Validate(battle, P1, vergil).Success);

            Assert.IsTrue(battle.Submit(new DevilTriggerCommand(P1, vergil)).Success);

            Assert.AreEqual(StarterCardLibrary.DevilTriggerId, vergil.Data.CardID);
            Assert.AreEqual(index, P1.Field.IndexOf(vergil));
            Assert.AreEqual(12, vergil.CurrentAttack);
            Assert.AreEqual(10, vergil.MaxHealth);
            Assert.AreEqual(8, vergil.CurrentHealth);
            Assert.IsTrue(vergil.HasKeyword(Keyword.Lifesteal));

            // ใช้ได้ครั้งเดียวต่อแมตช์
            Assert.IsFalse(DevilTriggerCommand.Validate(battle, P1, vergil).Success);

            battle.TurnManager.EndTurn(); // จบเทิร์นที่ 1 ของร่างนี้
            battle.TurnManager.EndTurn(); // เทิร์นของศัตรู
            Assert.AreEqual(StarterCardLibrary.DevilTriggerId, vergil.Data.CardID);

            // ต้นเทิร์นที่ 2: Sin Devil's Regeneration ฟื้น 3 (8 -> 10)
            Assert.AreEqual(10, vergil.CurrentHealth);

            battle.TurnManager.EndTurn(); // จบเทิร์นที่ 2 -> กลับร่าง

            Assert.AreEqual(StarterCardLibrary.VergilId, vergil.Data.CardID);
            Assert.AreEqual(6, vergil.MaxHealth);
            Assert.AreEqual(6, vergil.CurrentHealth); // ลดเหลือเพดานของร่างปกติ
            Assert.AreEqual(CardZone.Field, filler1.Zone);
        }

        [Test]
        public void DevilTrigger_NotAvailableOnSummonTurn()
        {
            CardInstance vergil = new CardInstance(
                StarterCardLibrary.Get(StarterCardLibrary.VergilId));

            vergil.OwnerID = P1.PlayerID;
            P1.Hand.Add(vergil);

            battle.Submit(new PlayCardCommand(P1, vergil));

            Assert.IsFalse(DevilTriggerCommand.Validate(battle, P1, vergil).Success);
        }

        [Test]
        public void OverwhelmingPower_IgnoresDefenseUpAndImmuneOnAttack()
        {
            CardInstance dt = Place(P1, StarterCardLibrary.DevilTriggerId);
            CardInstance target = Place(P2, "SUP_026");
            CardInstance control = Place(P2, "SUP_026");

            target.AddStatus(StatusType.DefenseUp, 3, 3);
            control.AddStatus(StatusType.DefenseUp, 3, 3);

            battle.DamageUnit(dt, target, 5);
            Assert.AreEqual(2, target.CurrentHealth);

            CardInstance normal = Place(P1, "SUP_026");

            battle.DamageUnit(normal, control, 5);
            Assert.AreEqual(5, control.CurrentHealth);
        }

        [Test]
        public void JudgmentOfTheEnd_HitsAllEnemiesWhenAttacking()
        {
            CardInstance dt = Place(P1, StarterCardLibrary.DevilTriggerId);
            CardInstance a = Place(P2, "SUP_026");
            CardInstance b = Place(P2, "SUP_026");

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, dt, a, null)).Success);

            Assert.AreNotEqual(CardZone.Field, a.Zone);
            Assert.AreEqual(4, b.CurrentHealth);
        }

        [Test]
        public void Concentration_GivesAttackOnlyIfNotDamaged()
        {
            CardInstance vergil = Place(P1, StarterCardLibrary.VergilId);

            battle.OnTurnStartPassives(P1);
            battle.Resolve();

            Assert.AreEqual(10, vergil.CurrentAttack);

            vergil.TurnAttackBonus = 0;
            battle.DamageUnit(null, vergil, 1);
            battle.Resolve();

            battle.OnTurnStartPassives(P1);
            battle.Resolve();

            Assert.AreEqual(8, vergil.CurrentAttack);
        }

        [Test]
        public void Unlock_NeedsTripleAttackerAndNoSpells()
        {
            BattleStats stats = battle.Stats;
            int h = P1.PlayerID;

            Assert.IsFalse(SecretCardService.IsVergilEarned(stats, h));

            stats.NoteSurvivingAttacks(h, 3);

            Assert.IsTrue(SecretCardService.IsVergilEarned(stats, h));

            stats.AddSpellPlayed(h);

            Assert.IsFalse(SecretCardService.IsVergilEarned(stats, h));
        }

        [Test]
        public void SameUnitAttackingThreeTimesAcrossTurnsCountsForUnlock()
        {
            P2.LeaderHealth = 99;
            P2.MaxLeaderHealth = 99;

            CardInstance vergil = Place(P1, StarterCardLibrary.VergilId);

            vergil.ExtraAttacks = 1;

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, vergil, null, P2)).Success);
            Assert.IsTrue(battle.Submit(new AttackCommand(P1, vergil, null, P2)).Success);

            Assert.IsFalse(battle.Stats.TripleAttacker[P1.PlayerID]);

            battle.TurnManager.EndTurn();
            battle.TurnManager.EndTurn();

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, vergil, null, P2)).Success);
            Assert.IsTrue(battle.Stats.TripleAttacker[P1.PlayerID]);
        }
    }
}
