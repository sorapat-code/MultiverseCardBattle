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
    /// <summary>ทดสอบสกิลพิเศษของ Usopp (ดาเมจเพิ่มเมื่อเป้า HP ≤ 2) และ Nami (จั่ว Spell แล้วลด Cost)</summary>
    public class RosterEffectTests
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
            filler.Health = 9;

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

        private CardInstance EnemyWithHealth(int health)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            data.CardID = "T_ENEMY";
            data.CardName = "T_ENEMY";
            data.Type = CardType.Unit;
            data.Cost = 1;
            data.Attack = 1;
            data.Health = health;
            data.Actions = new List<Effects.CardActionData>();

            CardInstance card = new CardInstance(data);

            card.OwnerID = P2.PlayerID;

            battle.SummonUnit(card, P2);

            card.SummonedOnTurn = -100;

            return card;
        }

        private CardInstance InHand(string id)
        {
            CardInstance card = new CardInstance(CardRegistry.Get(id));

            card.OwnerID = P1.PlayerID;
            P1.Hand.Add(card);

            return card;
        }

        private void PlayUsopp(CardInstance target)
        {
            CardInstance usopp = InHand("SUP_003");

            List<TargetCandidate> options =
                BattleRules.GetPlayTargets(battle, P1, usopp);

            TargetCandidate chosen = null;

            foreach (TargetCandidate option in options)
            {
                if (option.TargetObject == target)
                    chosen = option;
            }

            Assert.IsNotNull(chosen);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, usopp, chosen)).Success);
        }

        [Test]
        public void Usopp_DealsOneDamageToHealthyTarget()
        {
            CardInstance enemy = EnemyWithHealth(5);

            PlayUsopp(enemy);

            Assert.AreEqual(4, enemy.CurrentHealth);
        }

        [Test]
        public void Usopp_DealsExtraDamageToTargetWithTwoOrLessHealth()
        {
            CardInstance enemy = EnemyWithHealth(2);

            PlayUsopp(enemy);

            Assert.LessOrEqual(enemy.CurrentHealth, 0);
        }

        [Test]
        public void Usopp_NoBonusForThreeHealth()
        {
            CardInstance enemy = EnemyWithHealth(3);

            PlayUsopp(enemy);

            Assert.AreEqual(2, enemy.CurrentHealth);
        }

        private void StackDeck(params string[] ids)
        {
            P1.Deck.Clear();

            foreach (string id in ids)
                P1.Deck.Add(new CardInstance(CardRegistry.Get(id)));

            for (int i = 0; i < 5; i++)
                P1.Deck.Add(new CardInstance(filler));
        }

        [Test]
        public void Nami_DrawnSpellCostsOneLessUntilEndOfTurn()
        {
            StackDeck("MAG_004"); // Gale Slash, cost 3

            CardInstance nami = InHand("SUP_012");

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, nami)).Success);

            CardInstance drawn = null;

            foreach (CardInstance card in P1.Hand.Cards)
            {
                if (card.Data.CardID == "MAG_004")
                    drawn = card;
            }

            Assert.IsNotNull(drawn);
            Assert.AreEqual(2, drawn.CurrentCost);

            StatusProcessor.OnTurnEnd(battle, P1);
            battle.RefreshModifiers();

            Assert.AreEqual(3, drawn.CurrentCost);
        }

        [Test]
        public void Nami_DrawnUnitIsNotDiscounted()
        {
            StackDeck("SUP_005"); // Krillin, cost 3

            CardInstance nami = InHand("SUP_012");

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, nami)).Success);

            CardInstance drawn = null;

            foreach (CardInstance card in P1.Hand.Cards)
            {
                if (card.Data.CardID == "SUP_005")
                    drawn = card;
            }

            Assert.IsNotNull(drawn);
            Assert.AreEqual(3, drawn.CurrentCost);
        }
    }
}
