using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ทดสอบ Aemeath — Guiding Starlance: การสุ่มเปลี่ยนการ์ดศัตรู, Starlance Reset และเงื่อนไขปลดล็อก</summary>
    public class AemeathTests
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

            Assert.IsTrue(battle.HasStarted);
        }

        [TearDown]
        public void TearDown()
        {
            BattleManager.AemeathRandom = () => Random.value;

            Object.Destroy(host);
            Object.DestroyImmediate(filler);

            BattleSetup.Reset();
        }

        private CardInstance Place(BattlePlayer owner, CardData data)
        {
            CardInstance card = new CardInstance(data);

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            return card;
        }

        private CardInstance InHand(BattlePlayer owner)
        {
            CardInstance card = new CardInstance(filler);

            card.OwnerID = owner.PlayerID;

            owner.AddToZone(card, CardZone.Hand);

            return card;
        }

        // ---------- โอกาสปรากฏ ----------

        [Test]
        public void Chance_IsOnePercent_WithoutStage()
        {
            BattleSetup.Stage = null;

            Assert.AreEqual(0.01f, BattleManager.AemeathChance(), 0.0001f);
        }

        [Test]
        public void Chance_ScalesFromOnePercentToThreePercent_AcrossStages()
        {
            IReadOnlyList<CampaignStage> stages = CampaignLibrary.Stages;

            BattleSetup.Stage = stages[0];
            Assert.AreEqual(0.01f, BattleManager.AemeathChance(), 0.0001f);

            BattleSetup.Stage = stages[stages.Count - 1];
            Assert.AreEqual(0.03f, BattleManager.AemeathChance(), 0.0001f);

            BattleSetup.Stage = stages[stages.Count / 2];

            float mid = BattleManager.AemeathChance();

            Assert.Greater(mid, 0.01f);
            Assert.Less(mid, 0.03f);
        }

        [Test]
        public void Chance_IsZero_InTutorial()
        {
            BattleSetup.Tutorial = true;

            Assert.AreEqual(0f, BattleManager.AemeathChance(), 0.0001f);
        }

        // ---------- การเปลี่ยนการ์ด ----------

        [Test]
        public void EnemyCard_BecomesAemeath_WhenRollSucceeds()
        {
            BattleManager.AemeathRandom = () => 0f;

            CardInstance card = InHand(P2);

            Assert.IsTrue(battle.TryAemeathReplace(P2, card));
            Assert.AreEqual(StarterCardLibrary.AemeathId, card.Data.CardID);
            Assert.IsTrue(battle.AemeathAppeared);
        }

        [Test]
        public void EnemyCard_DoesNotChange_WhenRollFails()
        {
            BattleManager.AemeathRandom = () => 0.99f;

            CardInstance card = InHand(P2);

            Assert.IsFalse(battle.TryAemeathReplace(P2, card));
            Assert.AreEqual("FILLER", card.Data.CardID);
            Assert.IsFalse(battle.AemeathAppeared);
        }

        [Test]
        public void PlayerCards_NeverBecomeAemeath()
        {
            BattleManager.AemeathRandom = () => 0f;

            CardInstance card = InHand(P1);

            Assert.IsFalse(battle.TryAemeathReplace(P1, card));
            Assert.IsFalse(battle.AemeathAppeared);
        }

        [Test]
        public void Aemeath_AppearsOnlyOncePerGame()
        {
            BattleManager.AemeathRandom = () => 0f;

            Assert.IsTrue(battle.TryAemeathReplace(P2, InHand(P2)));
            Assert.IsFalse(battle.TryAemeathReplace(P2, InHand(P2)));
        }

        [Test]
        public void Aemeath_DoesNotAppear_WhenEnemyFieldIsFull()
        {
            BattleManager.AemeathRandom = () => 0f;

            for (int i = 0; i < BattlePlayer.MaxFieldSize; i++)
                Place(P2, filler);

            Assert.IsFalse(battle.TryAemeathReplace(P2, InHand(P2)));
        }

        // ---------- Starlance Reset ----------

        [Test]
        public void Summon_ResetsBothBoards_AndDrawsFive()
        {
            CardInstance mine = Place(P1, filler);
            CardInstance theirs = Place(P2, filler);

            InHand(P1);
            InHand(P2);

            CardInstance aemeath = Place(P1, StarterCardLibrary.Get(StarterCardLibrary.AemeathId));

            battle.Resolve();

            Assert.AreEqual(1, battle.StarlanceResets);

            Assert.AreEqual(1, P1.Field.Count);
            Assert.AreSame(aemeath, P1.Field[0]);
            Assert.AreEqual(0, P2.Field.Count);

            Assert.AreEqual(5, P1.Hand.Count);
            Assert.AreEqual(5, P2.Hand.Count);

            Assert.AreEqual(0, P1.Graveyard.Count);
            Assert.AreEqual(0, P2.Graveyard.Count);

            Assert.IsFalse(P1.Field.Contains(mine));
            Assert.IsFalse(P2.Field.Contains(theirs));
        }

        [Test]
        public void Summon_HealsBothLeaders_ToFull()
        {
            P1.LeaderHealth = 3;
            P2.LeaderHealth = 7;

            Place(P1, StarterCardLibrary.Get(StarterCardLibrary.AemeathId));

            battle.Resolve();

            Assert.AreEqual(P1.MaxLeaderHealth, P1.LeaderHealth);
            Assert.AreEqual(P2.MaxLeaderHealth, P2.LeaderHealth);
        }

        [Test]
        public void Summon_ResetsEnergy_ToOne_ForBothPlayers_AndRestartsGrowth()
        {
            P1.MaxEnergy = 9;
            P1.CurrentEnergy = 9;
            P2.MaxEnergy = 8;
            P2.CurrentEnergy = 8;

            Place(P1, StarterCardLibrary.Get(StarterCardLibrary.AemeathId));

            battle.Resolve();

            Assert.AreEqual(1, P1.MaxEnergy);
            Assert.AreEqual(1, P1.CurrentEnergy);
            Assert.AreEqual(1, P2.MaxEnergy);
            Assert.AreEqual(1, P2.CurrentEnergy);

            Assert.AreEqual(battle.TurnManager.TurnNumber - 1, P1.EnergyTurnBase);
            Assert.AreEqual(battle.TurnManager.TurnNumber - 1, P2.EnergyTurnBase);
        }

        [Test]
        public void Summon_ReturnsGraveyardToDeck_ButTokensVanish()
        {
            CardInstance dead = new CardInstance(filler);

            dead.OwnerID = P1.PlayerID;
            P1.AddToZone(dead, CardZone.Graveyard);

            CardData tokenData = ScriptableObject.CreateInstance<CardData>();

            tokenData.CardID = "TEST_TOKEN";
            tokenData.CardName = "TEST_TOKEN";
            tokenData.Type = CardType.Unit;
            tokenData.Source = CardSource.Token;
            tokenData.Health = 1;

            CardInstance token = new CardInstance(tokenData);

            token.OwnerID = P1.PlayerID;
            P1.AddToZone(token, CardZone.Graveyard);

            int before = P1.Deck.Count + P1.Hand.Count;

            Place(P1, StarterCardLibrary.Get(StarterCardLibrary.AemeathId));

            battle.Resolve();

            Assert.AreEqual(0, P1.Graveyard.Count);

            // การ์ดสามัญในสุสานกลับเข้ากอง (รวมกอง+มือเพิ่มขึ้น 1 ใบ) ส่วน Token ไม่กลับ
            Assert.AreEqual(before + 1, P1.Deck.Count + P1.Hand.Count);

            Object.DestroyImmediate(tokenData);
        }

        [Test]
        public void StarlanceReset_RestoresUnitsFromGraveyardToBaseStats()
        {
            CardInstance dead = new CardInstance(filler);

            dead.OwnerID = P1.PlayerID;
            dead.CurrentAttack = 99;
            dead.CurrentHealth = 0;
            P1.AddToZone(dead, CardZone.Graveyard);

            Place(P1, StarterCardLibrary.Get(StarterCardLibrary.AemeathId));

            battle.Resolve();

            Assert.AreEqual(filler.Attack, dead.CurrentAttack);
            Assert.AreEqual(filler.Health, dead.CurrentHealth);
        }

        // ---------- ปลดล็อก ----------

        [Test]
        public void Unlock_Granted_OnlyWhenAemeathAppearedAndPlayerWon()
        {
            PlayerProfile profile = new PlayerProfile(new SaveData());

            battle.AemeathAppeared = false;

            Assert.IsFalse(HasAemeathUnlock(
                SecretCardService.Evaluate(battle, P1.PlayerID, true, profile)));

            battle.AemeathAppeared = true;

            Assert.IsFalse(HasAemeathUnlock(
                SecretCardService.Evaluate(battle, P1.PlayerID, false, profile)));

            Assert.IsTrue(HasAemeathUnlock(
                SecretCardService.Evaluate(battle, P1.PlayerID, true, profile)));
        }

        [Test]
        public void Unlock_NotRepeated_WhenAlreadyOwned()
        {
            PlayerProfile profile = new PlayerProfile(new SaveData());

            profile.AddCard(StarterCardLibrary.AemeathId, 1);

            battle.AemeathAppeared = true;

            Assert.IsFalse(HasAemeathUnlock(
                SecretCardService.Evaluate(battle, P1.PlayerID, true, profile)));
        }

        private static bool HasAemeathUnlock(List<SecretUnlock> list)
        {
            foreach (SecretUnlock unlock in list)
            {
                if (unlock.CardId == StarterCardLibrary.AemeathId)
                    return true;
            }

            return false;
        }
    }
}
