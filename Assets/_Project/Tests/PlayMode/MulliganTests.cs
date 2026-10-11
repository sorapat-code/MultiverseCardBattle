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
    /// <summary>ทดสอบช่วงเปลี่ยนการ์ด (Mulligan) ก่อนเริ่มเทิร์นแรก</summary>
    public class MulliganTests
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

            BattleSetup.UseMulligan = true;

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
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);
            Object.DestroyImmediate(filler);

            BattleSetup.Reset();
        }

        [Test]
        public void Start_WaitsForMulliganBeforeFirstTurn()
        {
            Assert.IsTrue(battle.MulliganPending);
            Assert.IsTrue(battle.HasPendingChoice);
            Assert.AreEqual(0, battle.TurnManager.TurnNumber);
        }

        [Test]
        public void Mulligan_ReplacesChosenCardsAndKeepsCounts()
        {
            int hand = P1.Hand.Count;
            int deck = P1.Deck.Count;

            List<CardInstance> chosen = new List<CardInstance>();

            chosen.Add(P1.Hand.Cards[0]);
            chosen.Add(P1.Hand.Cards[1]);

            int replaced = battle.Mulligan(P1, chosen);

            Assert.AreEqual(2, replaced);
            Assert.AreEqual(hand, P1.Hand.Count);
            Assert.AreEqual(deck, P1.Deck.Count);

            foreach (CardInstance card in chosen)
                Assert.AreEqual(CardZone.Deck, card.Zone);
        }

        [Test]
        public void Mulligan_CoinCannotBeReplaced()
        {
            CardInstance coin = new CardInstance(
                StarterCardLibrary.Get(StarterCardLibrary.CoinId));

            Assert.IsFalse(BattleManager.CanMulligan(coin));

            coin.OwnerID = P1.PlayerID;
            P1.Hand.Add(coin);

            List<CardInstance> chosen = new List<CardInstance> { coin };

            Assert.AreEqual(0, battle.Mulligan(P1, chosen));
            Assert.IsTrue(P1.Hand.Contains(coin));
        }

        [Test]
        public void FinishMulligan_StartsTheFirstTurn()
        {
            battle.FinishMulligan(new List<CardInstance>());

            Assert.IsFalse(battle.MulliganPending);
            Assert.AreEqual(1, battle.TurnManager.TurnNumber);
        }

        [Test]
        public void FinishMulligan_AiSwapsExpensiveCards()
        {
            CardData big = ScriptableObject.CreateInstance<CardData>();

            big.CardID = "BIG";
            big.CardName = "BIG";
            big.Type = CardType.Unit;
            big.Cost = 7;
            big.Attack = 7;
            big.Health = 7;

            CardInstance card = new CardInstance(big);

            card.OwnerID = P2.PlayerID;
            P2.Hand.Add(card);

            battle.FinishMulligan(new List<CardInstance>());

            Assert.IsFalse(P2.Hand.Contains(card));

            Object.DestroyImmediate(big);
        }
    }
}
