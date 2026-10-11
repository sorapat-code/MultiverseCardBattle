using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบกฎเด็คหมดแพ้ทันที และเหตุการณ์สุ่ม Summon ได้การ์ดลับ
    /// </summary>
    public class DeckOutSecretTests
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
            filler.Health = 1;

            List<CardData> deck = new List<CardData>();

            for (int i = 0; i < 30; i++)
                deck.Add(filler);

            BattleSetup.PlayerDeck = deck;
            BattleSetup.EnemyDeck = new List<CardData>(deck);

            host = new GameObject("TestBattle");
            battle = host.AddComponent<BattleManager>();

            yield return null;

            Assert.IsTrue(battle.HasStarted);

            P1.Hand.Clear();
            P2.Hand.Clear();
            P1.CurrentEnergy = 10;
        }

        [TearDown]
        public void TearDown()
        {
            EffectExecutor.RandomValue = () => Random.value;

            Object.Destroy(host);
            Object.DestroyImmediate(filler);

            BattleSetup.Reset();
        }

        [Test]
        public void EmptyDeck_LosesImmediately()
        {
            P1.Deck.Clear();

            battle.Resolve();

            Assert.IsTrue(battle.IsOver);
            Assert.AreEqual(P2, battle.Winner);
        }

        [Test]
        public void BothDecksEmpty_IsDraw()
        {
            P1.Deck.Clear();
            P2.Deck.Clear();

            battle.Resolve();

            Assert.IsTrue(battle.IsOver);
            Assert.IsNull(battle.Winner);
        }

        [Test]
        public void Summon_WithLuckyRoll_GivesSecretCard()
        {
            EffectExecutor.RandomValue = () => 0f;

            CardInstance caller =
                new CardInstance(StarterCardLibrary.Get("SUP_001"));

            caller.OwnerID = P1.PlayerID;

            P1.Hand.Add(caller);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, caller)).Success);

            bool found = false;

            foreach (CardInstance unit in P1.Field)
            {
                if (unit.Data.CardID == StarterCardLibrary.SecretId)
                    found = true;
            }

            Assert.IsTrue(found);
            Assert.IsTrue(battle.SecretCardSummoned);
        }

        [Test]
        public void Summon_WithNormalRoll_GivesNoSecretCard()
        {
            EffectExecutor.RandomValue = () => 0.99f;

            CardInstance caller =
                new CardInstance(StarterCardLibrary.Get("SUP_001"));

            caller.OwnerID = P1.PlayerID;

            P1.Hand.Add(caller);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, caller)).Success);

            foreach (CardInstance unit in P1.Field)
                Assert.AreNotEqual(StarterCardLibrary.SecretId, unit.Data.CardID);

            Assert.IsFalse(battle.SecretCardSummoned);
        }

        [Test]
        public void Summon_ForAiNeverGivesSecretCard()
        {
            EffectExecutor.RandomValue = () => 0f;

            battle.TurnManager.EndTurn();

            P2.CurrentEnergy = 10;

            CardInstance caller =
                new CardInstance(StarterCardLibrary.Get("SUP_001"));

            caller.OwnerID = P2.PlayerID;

            P2.Hand.Add(caller);

            battle.Submit(new PlayCardCommand(P2, caller));

            foreach (CardInstance unit in P2.Field)
                Assert.AreNotEqual(StarterCardLibrary.SecretId, unit.Data.CardID);
        }

        private CardInstance PlaceCard(BattlePlayer owner, string id)
        {
            CardInstance card = new CardInstance(StarterCardLibrary.Get(id));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        [Test]
        public void SecretChance_IsFivePercent()
        {
            Assert.AreEqual(0.05f, EffectExecutor.SecretCardChance, 0.0001f);
        }

        [Test]
        public void Shadowbound_NeedsATokenToActivate()
        {
            CardInstance monarch = PlaceCard(P1, StarterCardLibrary.SecretId);

            battle.Resolve();

            // ตอนลงสนามได้ Shadow Soldier 2 ตัว จึงทำงานทันที
            Assert.IsTrue(monarch.ShadowActive);
            Assert.IsTrue(monarch.HasKeyword(Keyword.Unstoppable));

            foreach (CardInstance unit in new List<CardInstance>(P1.Field))
            {
                if (unit != monarch)
                    battle.KillUnit(unit);
            }

            battle.Resolve();

            Assert.IsFalse(monarch.ShadowActive);
            Assert.IsFalse(monarch.HasKeyword(Keyword.Unstoppable));
        }

        [Test]
        public void Shadowbound_UnitCannotBeAttacked_ButLeaderStillCan()
        {
            CardInstance monarch = PlaceCard(P2, StarterCardLibrary.SecretId);

            CardInstance attacker = PlaceCard(P1, "SUP_001");

            battle.Resolve();

            Assert.IsTrue(monarch.ShadowActive);

            List<MultiverseCardBattle.Targeting.TargetCandidate> targets =
                BattleRules.GetAttackTargets(battle, P1, attacker);

            foreach (MultiverseCardBattle.Targeting.TargetCandidate candidate in targets)
                Assert.AreNotEqual(monarch, candidate.TargetObject);
        }

        [Test]
        public void Monarch_SummonsShadowSoldierAtTurnStart()
        {
            CardInstance monarch = PlaceCard(P1, StarterCardLibrary.SecretId);

            int before = P1.Field.Count;

            battle.TurnManager.EndTurn();
            battle.TurnManager.EndTurn();

            Assert.AreEqual(P1, battle.CurrentPlayer);
            Assert.Greater(P1.Field.Count, before);
        }
    }
}
