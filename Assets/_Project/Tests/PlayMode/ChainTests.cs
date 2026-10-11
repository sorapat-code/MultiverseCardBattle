using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiverseCardBattle.Battle;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบ Keyword ใหม่ (Enrage, Retaliate, Frenzy, Elusive, Regenerate, Thorns,
    /// Lethal Strike, Overload, Echo) และระบบ Fast spell / เชน
    /// </summary>
    public class ChainTests
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

            CardData filler = MakeUnit("FILLER", 1, 1, 1, Keyword.None);

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

            // AI ตอบโต้ทุกครั้งที่ทำได้ (ไม่สุ่ม)
            AIPlanner.RandomValue = () => 0f;
        }

        [TearDown]
        public void TearDown()
        {
            AIPlanner.RandomValue = () => UnityEngine.Random.value;

            Object.Destroy(host);

            foreach (CardData data in created)
                Object.DestroyImmediate(data);

            created.Clear();

            BattleSetup.Reset();
        }

        // ---------- ตัวช่วย ----------

        private CardData MakeUnit(
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

        private CardData MakeSpell(
            string id, int cost, Keyword keywords, params CardActionData[] actions)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            data.CardID = id;
            data.CardName = id;
            data.Type = CardType.Spell;
            data.Cost = cost;
            data.Keywords = keywords;
            data.Actions = new List<CardActionData>(actions);

            created.Add(data);

            return data;
        }

        private static CardActionData Action(
            TargetRule rule, TargetSelection selection, params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = TriggerType.Play;
            action.Target = rule;
            action.Selection = selection;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private static EffectData Effect(EffectType type, int value)
        {
            EffectData effect = new EffectData();

            effect.Type = type;
            effect.Value = value;

            return effect;
        }

        private CardInstance PutOnField(
            BattlePlayer owner, string id, int attack, int health, Keyword keywords)
        {
            CardInstance card =
                new CardInstance(MakeUnit(id, 1, attack, health, keywords));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            battle.Resolve();

            return card;
        }

        private ActionResult Attack(
            BattlePlayer player, CardInstance attacker,
            CardInstance targetUnit, BattlePlayer targetLeader)
        {
            return battle.Submit(
                new AttackCommand(player, attacker, targetUnit, targetLeader));
        }

        // ---------- Keyword ใหม่ ----------

        [Test]
        public void Enrage_GainsAttackWhileDamaged()
        {
            CardInstance unit = PutOnField(P1, "E", 3, 5, Keyword.Enrage);

            Assert.AreEqual(3, unit.CurrentAttack);

            battle.DamageUnit(null, unit, 1);
            battle.Resolve();

            Assert.AreEqual(3 + BattleManager.EnrageBonus, unit.CurrentAttack);

            battle.HealUnit(null, unit, 5);
            battle.Resolve();

            Assert.AreEqual(3, unit.CurrentAttack);
        }

        [Test]
        public void Retaliate_StrikesBackEvenWhenKilledByQuickAttack()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 3, Keyword.QuickAttack);
            CardInstance defender = PutOnField(P2, "D", 3, 3, Keyword.Retaliate);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.Contains(defender, P2.Graveyard);
            Assert.Contains(attacker, P1.Graveyard);
        }

        [Test]
        public void Retaliate_StrikesBackWhileStunned()
        {
            CardInstance attacker = PutOnField(P1, "A", 2, 5, Keyword.None);
            CardInstance defender = PutOnField(P2, "D", 3, 5, Keyword.Retaliate);

            defender.AddStatus(StatusType.Stun, 0, 2);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.AreEqual(2, attacker.CurrentHealth);
        }

        [Test]
        public void Frenzy_SurvivingDamageGivesAttackOnce()
        {
            CardInstance unit = PutOnField(P1, "F", 2, 5, Keyword.Frenzy);

            battle.DamageUnit(null, unit, 1);
            battle.Resolve();

            Assert.AreEqual(2 + BattleManager.FrenzyBonus, unit.CurrentAttack);
            Assert.IsFalse(unit.HasKeyword(Keyword.Frenzy));

            battle.DamageUnit(null, unit, 1);
            battle.Resolve();

            Assert.AreEqual(2 + BattleManager.FrenzyBonus, unit.CurrentAttack);
        }

        [Test]
        public void Elusive_CannotBeAttackedWhileOwnerHasAnotherUnit()
        {
            CardInstance elusive = PutOnField(P2, "E", 2, 2, Keyword.Elusive);
            CardInstance other = PutOnField(P2, "O", 1, 3, Keyword.None);
            CardInstance attacker = PutOnField(P1, "A", 2, 2, Keyword.None);

            List<TargetCandidate> targets =
                BattleRules.GetAttackTargets(battle, P1, attacker);

            bool hasElusive = false, hasOther = false;

            foreach (TargetCandidate candidate in targets)
            {
                if (candidate.TargetObject == elusive) hasElusive = true;
                if (candidate.TargetObject == other) hasOther = true;
            }

            Assert.IsFalse(hasElusive);
            Assert.IsTrue(hasOther);

            // เหลือตัวเดียวแล้วโจมตีได้
            battle.KillUnit(other);
            battle.Resolve();

            targets = BattleRules.GetAttackTargets(battle, P1, attacker);

            hasElusive = false;

            foreach (TargetCandidate candidate in targets)
            {
                if (candidate.TargetObject == elusive) hasElusive = true;
            }

            Assert.IsTrue(hasElusive);
        }

        [Test]
        public void Regenerate_HealsAtTurnStart()
        {
            CardInstance unit = PutOnField(P1, "R", 2, 6, Keyword.Regenerate);

            battle.DamageUnit(null, unit, 4);

            StatusProcessor.OnTurnStart(battle, P1);

            Assert.AreEqual(6 - 4 + BattleManager.RegenerateAmount, unit.CurrentHealth);
        }

        [Test]
        public void Thorns_DamagesAttacker()
        {
            CardInstance attacker = PutOnField(P1, "A", 3, 5, Keyword.None);
            CardInstance defender = PutOnField(P2, "D", 1, 4, Keyword.Thorns);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            // ตอบโต้ 1 + หนาม 2
            Assert.AreEqual(5 - 1 - BattleManager.ThornsDamage, attacker.CurrentHealth);
        }

        [Test]
        public void LethalStrike_KillsTargetLeftWithLowHealth()
        {
            CardInstance attacker = PutOnField(P1, "A", 2, 5, Keyword.LethalStrike);
            CardInstance defender = PutOnField(P2, "D", 1, 5, Keyword.None);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            // 5 - 2 = 3 HP เหลือ (<= 3) จึงตาย
            Assert.Contains(defender, P2.Graveyard);
        }

        [Test]
        public void Overload_LocksEnergyForNextTurn()
        {
            CardData data = MakeUnit("O", 3, 5, 5, Keyword.Overload);

            data.OverloadAmount = 2;

            CardInstance card = new CardInstance(data);

            P1.Hand.Add(card);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.AreEqual(2, P1.OverloadedEnergy);
        }

        [Test]
        public void Echo_ReturnsOneCopyWithoutEcho()
        {
            CardData data = MakeUnit("EC", 1, 1, 1, Keyword.Echo);

            CardInstance card = new CardInstance(data);

            P1.Hand.Add(card);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.AreEqual(1, P1.Hand.Count);

            CardInstance copy = P1.Hand.Cards[0];

            Assert.AreSame(data, copy.Data);
            Assert.IsFalse(copy.HasKeyword(Keyword.Echo));

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, copy)).Success);

            Assert.AreEqual(0, P1.Hand.Count);
        }

        // ---------- Fast / เชน ----------

        [Test]
        public void FastSpell_CanBePlayedAnytime_EvenOnOpponentsTurn()
        {
            CardData shot = MakeSpell("SHOT", 1, Keyword.Fast,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 3)));

            CardInstance card = new CardInstance(shot);

            P2.Hand.Add(card);
            P2.CurrentEnergy = 5;

            PutOnField(P1, "T", 1, 5, Keyword.None);

            // ไม่ใช่เทิร์นของ P2 และไม่มีหน้าต่างตอบโต้ แต่ Fast spell เล่นได้ทุกจังหวะ
            Assert.IsTrue(battle.Submit(new PlayCardCommand(P2, card)).Success);
        }

        [Test]
        public void NonFastSpell_StillRequiresYourTurn()
        {
            CardData shot = MakeSpell("SLOW", 1, Keyword.None,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 3)));

            CardInstance card = new CardInstance(shot);

            P2.Hand.Add(card);
            P2.CurrentEnergy = 5;

            PutOnField(P1, "T", 1, 5, Keyword.None);

            Assert.IsFalse(battle.Submit(new PlayCardCommand(P2, card)).Success);
        }

        [Test]
        public void AiResponse_KillsAttackerBeforeItsAttackResolves()
        {
            CardInstance attacker = PutOnField(P1, "A", 2, 2, Keyword.None);

            CardData shot = MakeSpell("SHOT", 2, Keyword.Fast,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 5)));

            P2.Hand.Add(new CardInstance(shot));
            P2.CurrentEnergy = 5;

            int leaderBefore = P2.LeaderHealth;

            Assert.IsTrue(Attack(P1, attacker, null, P2).Success);

            Assert.Contains(attacker, P1.Graveyard);
            Assert.AreEqual(leaderBefore, P2.LeaderHealth);
            Assert.IsFalse(battle.ChainBlocksActions);
        }

        [Test]
        public void AiNegate_CancelsAnExpensiveCard()
        {
            CardData negate = MakeSpell("NEG", 2, Keyword.Fast,
                Action(TargetRule.Self, TargetSelection.All,
                    Effect(EffectType.Negate, 0)));

            P2.Hand.Add(new CardInstance(negate));
            P2.CurrentEnergy = 5;

            CardInstance big = new CardInstance(MakeUnit("BIG", 3, 4, 4, Keyword.None));

            P1.Hand.Add(big);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, big)).Success);

            Assert.Contains(big, P1.Graveyard);
            Assert.IsFalse(P1.Field.Contains(big));
        }

        [Test]
        public void HumanResponse_NegatesOpponentPlay_OnTheirTurn()
        {
            CardData negate = MakeSpell("NEG", 2, Keyword.Fast,
                Action(TargetRule.Self, TargetSelection.All,
                    Effect(EffectType.Negate, 0)));

            CardInstance counter = new CardInstance(negate);

            P1.Hand.Add(counter);
            P1.CurrentEnergy = 5;

            // ถึงเทิร์นของ P2 (Energy ที่เหลือของ P1 ยังอยู่)
            Assert.IsTrue(battle.Submit(new EndTurnCommand(P1)).Success);

            Assert.AreEqual(P2, battle.CurrentPlayer);

            P2.Hand.Clear();
            P2.CurrentEnergy = 10;

            CardInstance unit = new CardInstance(MakeUnit("U", 2, 2, 2, Keyword.None));

            P2.Hand.Add(unit);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P2, unit)).Success);

            // รอ P1 ตอบโต้
            Assert.AreEqual(P1, battle.ChainResponder);
            Assert.IsFalse(P2.Field.Contains(unit));

            // ระหว่างหน้าต่างตอบโต้ ทำอย่างอื่นไม่ได้
            Assert.IsFalse(battle.Submit(new EndTurnCommand(P1)).Success);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, counter)).Success);

            Assert.IsNull(battle.ChainResponder);
            Assert.Contains(unit, P2.Graveyard);
            Assert.AreEqual(5 - 2, P1.CurrentEnergy);
        }

        [Test]
        public void HumanResponse_PassLetsTheActionResolve()
        {
            CardData shot = MakeSpell("SHOT", 1, Keyword.Fast,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 1)));

            P1.Hand.Add(new CardInstance(shot));
            P1.CurrentEnergy = 5;

            PutOnField(P2, "T", 1, 5, Keyword.None);

            Assert.IsTrue(battle.Submit(new EndTurnCommand(P1)).Success);

            P2.Hand.Clear();
            P2.CurrentEnergy = 10;

            CardInstance unit = new CardInstance(MakeUnit("U", 2, 2, 2, Keyword.None));

            P2.Hand.Add(unit);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P2, unit)).Success);
            Assert.AreEqual(P1, battle.ChainResponder);

            Assert.IsTrue(battle.Submit(new PassChainCommand(P1)).Success);

            Assert.IsNull(battle.ChainResponder);
            Assert.Contains(unit, P2.Field);
        }

        [Test]
        public void Chain_ResolvesLastCardFirst()
        {
            // P1 ลงยูนิต -> P2 (AI) ตอบด้วยดาเมจใส่ยูนิตนั้นไม่ได้ (ยังไม่ลง) จึงทดสอบด้วย Negate ซ้อน Negate
            CardData negateA = MakeSpell("NEGA", 1, Keyword.Fast,
                Action(TargetRule.Self, TargetSelection.All,
                    Effect(EffectType.Negate, 0)));

            CardData negateB = MakeSpell("NEGB", 1, Keyword.Fast,
                Action(TargetRule.Self, TargetSelection.All,
                    Effect(EffectType.Negate, 0)));

            // P2 (AI) มี Negate 1 ใบ  P1 มี Negate 1 ใบสำหรับตอบกลับ
            P2.Hand.Add(new CardInstance(negateA));
            P2.CurrentEnergy = 5;

            CardInstance counter = new CardInstance(negateB);

            P1.Hand.Add(counter);

            CardInstance big = new CardInstance(MakeUnit("BIG", 3, 4, 4, Keyword.None));

            P1.Hand.Add(big);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, big)).Success);

            // AI ใช้ Negate กับ BIG แล้วรอ P1 ตอบโต้
            Assert.AreEqual(P1, battle.ChainResponder);
            Assert.AreEqual(2, battle.ChainStack.Count);

            // P1 Negate การ์ดของ AI -> BIG กลับมาทำงานได้
            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, counter)).Success);

            Assert.IsNull(battle.ChainResponder);
            Assert.Contains(big, P1.Field);
        }
    }
}
