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
    /// <summary>ทดสอบสกิลพิเศษของ Usopp (ดาเมจเพิ่มเมื่อเป้า HP ≤ 2), Nami (จั่ว Spell แล้วลด Cost), Dan Heng และคู่ Kiana / Mei</summary>
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

        private CardInstance Place(BattlePlayer owner, string id)
        {
            CardInstance card = new CardInstance(CardRegistry.Get(id));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        [Test]
        public void DanHeng_FullHealthTarget_TakesExtraTwoDamage()
        {
            CardInstance enemy = EnemyWithHealth(9);
            CardInstance danHeng = Place(P1, "SUP_027");

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, danHeng, enemy, null)).Success);

            // 2 (โบนัส) + 4 (ATK) = 6
            Assert.AreEqual(3, enemy.CurrentHealth);
        }

        [Test]
        public void DanHeng_DamagedTarget_NoExtraDamage()
        {
            CardInstance enemy = EnemyWithHealth(9);

            enemy.CurrentHealth = 8;

            CardInstance danHeng = Place(P1, "SUP_027");

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, danHeng, enemy, null)).Success);

            Assert.AreEqual(4, enemy.CurrentHealth);
        }

        [Test]
        public void DanHeng_WithMarch7th_GainsShieldAfterFirstAttack()
        {
            Place(P1, "SUP_008");

            CardInstance enemy = EnemyWithHealth(9);
            CardInstance danHeng = Place(P1, "SUP_027");

            Assert.IsFalse(danHeng.HasKeyword(Keyword.Shield));

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, danHeng, enemy, null)).Success);

            Assert.IsTrue(danHeng.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void DanHeng_WithoutMarch7th_GetsNoShield()
        {
            CardInstance enemy = EnemyWithHealth(9);
            CardInstance danHeng = Place(P1, "SUP_027");

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, danHeng, enemy, null)).Success);

            Assert.IsFalse(danHeng.HasKeyword(Keyword.Shield));
        }

        private CardInstance PlayKiana(CardInstance target)
        {
            CardInstance kiana = InHand("SUP_028");

            List<TargetCandidate> options = BattleRules.GetPlayTargets(battle, P1, kiana);

            TargetCandidate chosen = null;

            foreach (TargetCandidate option in options)
            {
                if (option.TargetObject == target)
                    chosen = option;
            }

            Assert.IsNotNull(chosen);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, kiana, chosen)).Success);

            return kiana;
        }

        [Test]
        public void Kiana_Battlecry_DamagesBurnsAndTriggersBurnImmediately()
        {
            CardInstance enemy = EnemyWithHealth(9);

            CardInstance kiana = PlayKiana(enemy);

            // 3 (Battlecry) + 2 (Burn ทำงานทันที) = 5
            Assert.AreEqual(4, enemy.CurrentHealth);

            // Burn ยังอยู่ครบ 2 เทิร์น (ไม่ลดจากการสั่งให้ทำงานทันที)
            Assert.IsTrue(enemy.HasStatus(StatusType.Burn));
            Assert.AreEqual(2, enemy.Statuses.Find(x => x.Type == StatusType.Burn).Duration);

            Assert.IsFalse(kiana.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void Kiana_WithMeiOnField_GainsShieldAndMeiGetsAttackUpAndHeal()
        {
            CardInstance mei = Place(P1, "SUP_029");
            int attackBefore = mei.CurrentAttack;

            P1.LeaderHealth = 20;

            CardInstance kiana = PlayKiana(EnemyWithHealth(9));

            battle.RefreshModifiers();

            Assert.IsTrue(kiana.HasKeyword(Keyword.Shield));
            Assert.AreEqual(attackBefore + 2, mei.CurrentAttack);
            Assert.AreEqual(22, P1.LeaderHealth);
        }

        [Test]
        public void TogetherAgain_FirstUnitAttackOfTheTurnShieldsThePartnerOnce()
        {
            CardInstance kiana = Place(P1, "SUP_028");
            CardInstance mei = Place(P1, "SUP_029");

            CardInstance enemy1 = EnemyWithHealth(9);
            CardInstance enemy2 = EnemyWithHealth(9);

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, kiana, enemy1, null)).Success);

            Assert.IsTrue(mei.HasKeyword(Keyword.Shield));
            Assert.IsFalse(kiana.HasKeyword(Keyword.Shield));

            mei.RemoveKeyword(Keyword.Shield);

            // Mei โจมตีทีหลัง: ไม่ได้ครั้งแรกของคู่แล้ว Kiana ไม่ได้ Shield
            Assert.IsTrue(battle.Submit(new AttackCommand(P1, mei, enemy2, null)).Success);

            Assert.IsFalse(kiana.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void TogetherAgain_AttackingTheLeaderDoesNotCount()
        {
            CardInstance kiana = Place(P1, "SUP_028");
            CardInstance mei = Place(P1, "SUP_029");

            Assert.IsTrue(battle.Submit(new AttackCommand(P1, kiana, null, P2)).Success);

            Assert.IsFalse(mei.HasKeyword(Keyword.Shield));
        }
    }
}
