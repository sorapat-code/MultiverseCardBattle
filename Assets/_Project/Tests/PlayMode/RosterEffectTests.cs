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
    /// <summary>ทดสอบสกิลพิเศษของ Usopp (ดาเมจเพิ่มเมื่อเป้า HP ≤ 2), Nami (จั่ว Spell แล้วลด Cost), Dan Heng, คู่ Kiana / Mei, Lelouch, Guts, Raiden Shogun และ Zhongli</summary>
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
        public void Kiana_Battlecry_WithoutBurn_JustDealsThreeDamage()
        {
            CardInstance enemy = EnemyWithHealth(9);

            CardInstance kiana = PlayKiana(enemy);

            Assert.AreEqual(6, enemy.CurrentHealth);
            Assert.IsFalse(enemy.HasStatus(StatusType.Burn));
            Assert.IsFalse(kiana.HasKeyword(Keyword.Shield));
        }

        [Test]
        public void Kiana_Battlecry_TriggersExistingBurnImmediatelyWithoutReducingItsTurns()
        {
            CardInstance enemy = EnemyWithHealth(9);

            enemy.AddStatus(StatusType.Burn, 2, 2);

            PlayKiana(enemy);

            // 3 (Battlecry) + 2 (Burn ทำงานทันที) = 5
            Assert.AreEqual(4, enemy.CurrentHealth);

            // Burn ยังอยู่ครบ 2 เทิร์น
            Assert.IsTrue(enemy.HasStatus(StatusType.Burn));
            Assert.AreEqual(2, enemy.Statuses.Find(x => x.Type == StatusType.Burn).Duration);
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

        [Test]
        public void Lelouch_Battlecry_StunsChosenEnemyUnit()
        {
            CardInstance enemy = EnemyWithHealth(9);
            CardInstance lelouch = InHand("SUP_030");

            List<TargetCandidate> options = BattleRules.GetPlayTargets(battle, P1, lelouch);

            TargetCandidate chosen = null;

            foreach (TargetCandidate option in options)
            {
                if (option.TargetObject == enemy)
                    chosen = option;
            }

            Assert.IsNotNull(chosen);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, lelouch, chosen)).Success);

            Assert.IsTrue(enemy.IsStunned);
        }

        [Test]
        public void Lelouch_TurnStart_GeassStunsEnemyUnit()
        {
            Place(P1, "SUP_030");

            CardInstance enemy = EnemyWithHealth(9);

            battle.OnTurnStartPassives(P1);

            // มนุษย์ต้องคลิกเลือก / AI สุ่มเอง (ตัวเลือกเดียวจึงได้ตัวเดิม)
            if (battle.HasPendingTarget)
                Assert.IsTrue(battle.ResolvePendingTarget(enemy));

            Assert.IsTrue(enemy.IsStunned);
        }

        [Test]
        public void Lelouch_Chessmaster_SecondCardOfTheTurnDrawsOne()
        {
            Place(P1, "SUP_030");

            CardInstance first = InHand("SUP_006");   // Connie, cost 1
            CardInstance second = InHand("SUP_007");  // Sasha, cost 2

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, first)).Success);

            // ใบแรก: ยังไม่จั่ว
            Assert.AreEqual(1, P1.Hand.Count);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, second)).Success);

            // ใบที่สอง: เล่นออก 1 ใบ จั่วเข้ามา 1 ใบ
            Assert.AreEqual(1, P1.Hand.Count);
        }

        [Test]
        public void Guts_DroppingToTwoHealthGrantsAttackAndUnstoppableUntilEndOfTurn()
        {
            CardInstance guts = Place(P1, "SUP_032");
            CardInstance enemy = EnemyWithHealth(9);

            battle.DamageUnit(enemy, guts, 1);   // 5 -> 4: ยังไม่ถึง

            battle.RefreshModifiers();

            Assert.AreEqual(6, guts.CurrentAttack);
            Assert.IsFalse(guts.HasKeyword(Keyword.Unstoppable));

            battle.DamageUnit(enemy, guts, 2);   // 4 -> 2

            battle.RefreshModifiers();

            Assert.AreEqual(9, guts.CurrentAttack);
            Assert.IsTrue(guts.HasKeyword(Keyword.Unstoppable));

            // โดนซ้ำตอนยังอยู่ในสถานะนี้ ไม่ซ้อนทับ
            battle.DamageUnit(enemy, guts, 1);   // 2 -> 1

            battle.RefreshModifiers();

            Assert.AreEqual(9, guts.CurrentAttack);
        }

        private CardInstance SpellInHand(string id)
        {
            return InHand(id);
        }

        [Test]
        public void Raiden_SpellburstOnlyUsedByAThreeCostSpell_AndRestoresOneEnergy()
        {
            CardInstance raiden = Place(P1, "SUP_033");

            P1.CurrentEnergy = 10;

            // Spell Cost 2: ไม่เข้าเงื่อนไข ไม่ฟื้น Energy และยังเก็บ Spellburst ไว้
            CardInstance cheap = SpellInHand("MAG_006");

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, cheap)).Success);

            Assert.AreEqual(8, P1.CurrentEnergy);
            Assert.IsTrue(raiden.HasKeyword(Keyword.Spellburst));

            // Spell Cost 3: ฟื้น 1 (8 - 3 + 1) แล้ว Spellburst หมดไป
            CardInstance big = SpellInHand("MAG_004");

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, big)).Success);

            Assert.AreEqual(6, P1.CurrentEnergy);
            Assert.IsFalse(raiden.HasKeyword(Keyword.Spellburst));
        }

        [Test]
        public void Zhongli_AuraReducesDamageToOtherAllies_ButNotToHimself()
        {
            CardInstance zhongli = Place(P1, "SUP_034");
            CardInstance ally = Place(P1, "SUP_001");
            CardInstance enemy = EnemyWithHealth(9);

            ally.Keywords = Keyword.None;

            int allyBefore = ally.CurrentHealth;
            int zhongliBefore = zhongli.CurrentHealth;

            zhongli.RemoveKeyword(Keyword.Shield);

            battle.DamageUnit(enemy, ally, 3);
            battle.DamageUnit(enemy, zhongli, 3);

            Assert.AreEqual(allyBefore - 2, ally.CurrentHealth);
            Assert.AreEqual(zhongliBefore - 3, zhongli.CurrentHealth);
        }

        [Test]
        public void Zhongli_FirstShieldBreakEachTurnHealsLeaderOnce()
        {
            Place(P1, "SUP_034");

            CardInstance allyA = Place(P1, "SUP_001");
            CardInstance allyB = Place(P1, "SUP_002");
            CardInstance enemy = EnemyWithHealth(9);

            allyA.AddKeyword(Keyword.Shield);
            allyB.AddKeyword(Keyword.Shield);

            P1.LeaderHealth = 20;

            battle.DamageUnit(enemy, allyA, 3);

            Assert.AreEqual(21, P1.LeaderHealth);

            // ครั้งที่สองในเทิร์นเดียวกัน ไม่ฟื้นอีก
            battle.DamageUnit(enemy, allyB, 3);

            Assert.AreEqual(21, P1.LeaderHealth);
        }

        [Test]
        public void Zhongli_Battlecry_GivesShieldToChosenAlly()
        {
            CardInstance ally = Place(P1, "SUP_001");
            CardInstance zhongli = InHand("SUP_034");

            List<TargetCandidate> options = BattleRules.GetPlayTargets(battle, P1, zhongli);

            TargetCandidate chosen = null;

            foreach (TargetCandidate option in options)
            {
                if (option.TargetObject == ally)
                    chosen = option;
            }

            Assert.IsNotNull(chosen);

            ally.Keywords = Keyword.None;

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, zhongli, chosen)).Success);

            Assert.IsTrue(ally.HasKeyword(Keyword.Shield));
        }
    }
}
