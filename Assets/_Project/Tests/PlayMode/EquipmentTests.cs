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
    /// <summary>ทดสอบระบบอุปกรณ์ (Unit gear / Leader gear) และ Backstab</summary>
    public class EquipmentTests
    {
        private GameObject host;
        private BattleManager battle;
        private CardData filler;

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

        private CardInstance InHand(string id)
        {
            CardInstance card = new CardInstance(StarterCardLibrary.Get(id));

            card.OwnerID = P1.PlayerID;
            P1.Hand.Add(card);

            return card;
        }

        [Test]
        public void UnitGear_AddsStatsAndKeywordsToChosenUnit()
        {
            CardInstance unit = Place(P1, "STR_001"); // 2/2

            CardInstance gear = InHand("RFT_049"); // Excalibur +4/+2 Shield Lifesteal

            List<TargetCandidate> options =
                BattleRules.GetPlayTargets(battle, P1, gear);

            Assert.AreEqual(1, options.Count);

            Assert.IsTrue(battle.Submit(
                new PlayCardCommand(P1, gear, options[0])).Success);

            Assert.AreEqual(6, unit.CurrentAttack);
            Assert.AreEqual(4, unit.CurrentHealth);
            Assert.IsTrue(unit.HasKeyword(Keyword.Shield));
            Assert.IsTrue(unit.HasKeyword(Keyword.Lifesteal));
            Assert.AreEqual(CardZone.Graveyard, gear.Zone);
        }

        [Test]
        public void UnitGear_CannotBePlayedWithoutAFriendlyUnit()
        {
            CardInstance gear = InHand("RFT_043");

            Assert.IsFalse(
                BattleRules.ValidatePlayable(battle, P1, gear).Success);
        }

        [Test]
        public void LeaderGear_StaysAndTicksDownThenExpires()
        {
            CardInstance gear = InHand("RFT_047"); // Chaldea Mystic Code, 4 turns

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, gear)).Success);

            Assert.AreEqual(gear, P1.LeaderGear);
            Assert.AreEqual(4, P1.LeaderGearTurns);

            for (int i = 0; i < 4; i++)
                battle.TickLeaderGear(P1);

            Assert.IsNull(P1.LeaderGear);
            Assert.AreEqual(CardZone.Graveyard, gear.Zone);
        }

        [Test]
        public void LeaderGear_HealsAtTurnStart()
        {
            CardInstance gear = InHand("RFT_047");

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, gear)).Success);

            P1.LeaderHealth = P1.MaxLeaderHealth - 5;

            int before = P1.LeaderHealth;

            battle.TurnManager.EndTurn();
            battle.TurnManager.EndTurn();

            Assert.AreEqual(before + 2, P1.LeaderHealth);
        }

        [Test]
        public void Backstab_DestroysAnyUnitThatTookDamage()
        {
            CardInstance source = Place(P1, "STR_008"); // Disaster Curse
            CardInstance big = Place(P2, "STR_005");

            Assert.IsTrue(source.HasKeyword(Keyword.Backstab));

            battle.DamageUnit(source, big, 1);
            battle.Resolve();

            Assert.AreNotEqual(CardZone.Field, big.Zone);
        }
    }
}
