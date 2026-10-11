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
    /// ทดสอบ Trigger ใหม่: ถูกเล็งเป้า (OnTarget พร้อมตัวกรอง), ลงสุสาน (Graveyard)
    /// และเอฟเฟกต์ดูหัวกอง (LookTop)
    /// </summary>
    public class TriggerSetTests
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

        // ---------- ตัวช่วยเพิ่มเติม ----------

        private static CardActionData Triggered(
            TriggerType trigger, TargetRule rule, params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = trigger;
            action.Target = rule;
            action.Selection = TargetSelection.All;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private CardInstance PutWithActions(
            BattlePlayer owner, string id, int attack, int health,
            params CardActionData[] actions)
        {
            CardData data = MakeUnit(id, 1, attack, health, Keyword.None);

            data.Actions = new List<CardActionData>(actions);

            CardInstance card = new CardInstance(data);

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            battle.Resolve();

            return card;
        }

        private static TargetCandidate Candidate(CardInstance card)
        {
            return new TargetCandidate(card.OwnerID, false, true, card);
        }

        // ---------- ถูกเล็งเป้า ----------

        [Test]
        public void Targeted_ByEnemyAttack_HurtsTheAttacker()
        {
            CardActionData reaction =
                Triggered(TriggerType.OnTarget, TargetRule.EventSource,
                    Effect(EffectType.DealDamage, 2));

            reaction.TargetedBy = TargetedSource.Attack;
            reaction.TargetedFrom = TargetedSide.Enemy;

            CardInstance attacker = PutOnField(P1, "A", 1, 5, Keyword.None);
            CardInstance defender = PutWithActions(P2, "D", 1, 5, reaction);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            // 5 - 1 (สวนกลับ) - 2 (ถูกเล็งเป้า)
            Assert.AreEqual(2, attacker.CurrentHealth);
        }

        [Test]
        public void Targeted_SpellOnlyReaction_IgnoresAttacks()
        {
            CardActionData reaction =
                Triggered(TriggerType.OnTarget, TargetRule.Self,
                    Effect(EffectType.GainAttack, 2));

            reaction.TargetedBy = TargetedSource.SpellOrAbility;

            CardInstance attacker = PutOnField(P1, "A", 1, 5, Keyword.None);
            CardInstance defender = PutWithActions(P2, "D", 1, 5, reaction);

            Assert.IsTrue(Attack(P1, attacker, defender, null).Success);

            Assert.AreEqual(1, defender.CurrentAttack);
        }

        [Test]
        public void Targeted_BySpell_TriggersSpellReaction()
        {
            CardActionData reaction =
                Triggered(TriggerType.OnTarget, TargetRule.Self,
                    Effect(EffectType.GainAttack, 2));

            reaction.TargetedBy = TargetedSource.SpellOrAbility;
            reaction.TargetedFrom = TargetedSide.Enemy;

            CardInstance defender = PutWithActions(P2, "D", 1, 9, reaction);

            CardData bolt = MakeSpell("BOLT", 1, Keyword.None,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 1)));

            CardInstance card = new CardInstance(bolt);

            P1.Hand.Add(card);

            Assert.IsTrue(
                battle.Submit(new PlayCardCommand(P1, card, Candidate(defender))).Success);

            Assert.AreEqual(3, defender.CurrentAttack);
        }

        [Test]
        public void Targeted_AttackReaction_IgnoresSpells()
        {
            CardActionData reaction =
                Triggered(TriggerType.OnTarget, TargetRule.Self,
                    Effect(EffectType.GainAttack, 2));

            reaction.TargetedBy = TargetedSource.Attack;

            CardInstance defender = PutWithActions(P2, "D", 1, 9, reaction);

            CardData bolt = MakeSpell("BOLT", 1, Keyword.None,
                Action(TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Effect(EffectType.DealDamage, 1)));

            CardInstance card = new CardInstance(bolt);

            P1.Hand.Add(card);

            Assert.IsTrue(
                battle.Submit(new PlayCardCommand(P1, card, Candidate(defender))).Success);

            Assert.AreEqual(1, defender.CurrentAttack);
        }

        // ---------- ลงสุสาน ----------

        [Test]
        public void Graveyard_FiresWhenUnitDies()
        {
            CardInstance martyr = PutWithActions(P1, "M", 1, 1,
                Triggered(TriggerType.Graveyard, TargetRule.EnemyLeader,
                    Effect(EffectType.DealDamage, 2)));

            int before = P2.LeaderHealth;

            battle.KillUnit(martyr);
            battle.Resolve();

            Assert.AreEqual(before - 2, P2.LeaderHealth);
        }

        [Test]
        public void Graveyard_FiresWhenDiscarded()
        {
            CardData data = MakeUnit("H", 1, 1, 1, Keyword.None);

            data.Actions = new List<CardActionData>
            {
                Triggered(TriggerType.Graveyard, TargetRule.EnemyLeader,
                    Effect(EffectType.DealDamage, 3))
            };

            CardInstance card = new CardInstance(data);

            P1.Hand.Add(card);

            int before = P2.LeaderHealth;

            battle.DiscardCard(P1, card);
            battle.Resolve();

            Assert.AreEqual(before - 3, P2.LeaderHealth);
        }

        [Test]
        public void Graveyard_FiresWhenSpellFinishes()
        {
            CardData data = MakeSpell("GIFT", 1, Keyword.None,
                Triggered(TriggerType.Graveyard, TargetRule.EnemyLeader,
                    Effect(EffectType.DealDamage, 4)));

            CardInstance card = new CardInstance(data);

            P1.Hand.Add(card);

            int before = P2.LeaderHealth;

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.AreEqual(before - 4, P2.LeaderHealth);
        }

        // ---------- ดูหัวกอง ----------

        private void SetDeck(BattlePlayer player, params CardData[] top)
        {
            player.Deck.Clear();

            foreach (CardData data in top)
                player.Deck.Add(new CardInstance(data));

            // กองสำรองกันเด็คหมด
            for (int i = 0; i < 5; i++)
                player.Deck.Add(new CardInstance(MakeUnit("PAD", 1, 1, 1, Keyword.None)));
        }

        private static EffectData Look(int count, PickFilter filter, int maxCost = 0)
        {
            EffectData effect = Effect(EffectType.LookTop, count);

            effect.PickType = filter;
            effect.PickMaxCost = maxCost;

            return effect;
        }

        [Test]
        public void LookTop_Human_PicksFromEligibleCards()
        {
            CardData unitA = MakeUnit("UA", 1, 1, 1, Keyword.None);
            CardData spellB = MakeSpell("SB", 1, Keyword.None);
            CardData unitC = MakeUnit("UC", 1, 1, 1, Keyword.None);

            SetDeck(P1, unitA, spellB, unitC);

            CardData look = MakeSpell("LOOK", 1, Keyword.None,
                Triggered(TriggerType.Play, TargetRule.AllyLeader,
                    Look(3, PickFilter.Spell)));

            CardInstance card = new CardInstance(look);

            P1.Hand.Add(card);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.IsTrue(battle.HasPendingDiscover);
            Assert.AreEqual(1, battle.PendingDiscover.Result.Options.Count);
            Assert.AreEqual(spellB, battle.PendingDiscover.Result.Options[0]);

            int deckBefore = P1.Deck.Count;

            Assert.IsTrue(battle.ResolveDiscover(spellB));

            Assert.IsFalse(battle.HasPendingDiscover);
            Assert.AreEqual(deckBefore - 1, P1.Deck.Count);
            Assert.AreEqual(1, P1.Hand.Count);
            Assert.AreEqual(spellB, P1.Hand.Cards[0].Data);

            // ใบที่ดูแล้วไม่ได้เลือกถูกย้ายไปล่างกอง
            Assert.AreEqual("PAD", P1.Deck.Cards[0].Data.CardID);
            Assert.AreEqual("UC", P1.Deck.Cards[P1.Deck.Count - 1].Data.CardID);
        }

        [Test]
        public void LookTop_NothingMatches_LeavesDeckAlone()
        {
            SetDeck(P1,
                MakeUnit("UA", 1, 1, 1, Keyword.None),
                MakeUnit("UB", 1, 1, 1, Keyword.None));

            CardData look = MakeSpell("LOOK", 1, Keyword.None,
                Triggered(TriggerType.Play, TargetRule.AllyLeader,
                    Look(2, PickFilter.Spell)));

            CardInstance card = new CardInstance(look);

            P1.Hand.Add(card);

            int deckBefore = P1.Deck.Count;

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.IsFalse(battle.HasPendingDiscover);
            Assert.AreEqual(deckBefore, P1.Deck.Count);
            Assert.AreEqual("UA", P1.Deck.Cards[0].Data.CardID);
        }

        [Test]
        public void LookTop_CostFilter_OnlyOffersCheapCards()
        {
            CardData cheap = MakeUnit("CHEAP", 2, 1, 1, Keyword.None);
            CardData pricey = MakeUnit("PRICEY", 6, 1, 1, Keyword.None);

            SetDeck(P1, pricey, cheap);

            CardData look = MakeSpell("LOOK", 1, Keyword.None,
                Triggered(TriggerType.Play, TargetRule.AllyLeader,
                    Look(2, PickFilter.Any, 3)));

            CardInstance card = new CardInstance(look);

            P1.Hand.Add(card);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, card)).Success);

            Assert.AreEqual(1, battle.PendingDiscover.Result.Options.Count);
            Assert.AreEqual(cheap, battle.PendingDiscover.Result.Options[0]);
        }

        [Test]
        public void LookTop_Ai_TakesTheMostExpensiveEligibleCard()
        {
            CardData cheap = MakeUnit("CHEAP", 1, 1, 1, Keyword.None);
            CardData pricey = MakeUnit("PRICEY", 5, 1, 1, Keyword.None);

            SetDeck(P2, cheap, pricey);

            P2.Hand.Clear();

            battle.RequestLookTop(P2, 2, PickFilter.Any, 0);

            Assert.IsFalse(battle.HasPendingDiscover);
            Assert.AreEqual(1, P2.Hand.Count);
            Assert.AreEqual(pricey, P2.Hand.Cards[0].Data);
        }

        [Test]
        public void StarterLibrary_TriggerSetCardsExist()
        {
            foreach (string id in new[]
            {
                "TGT_001", "TGT_007", "GRV_001", "GRV_007", "LKT_001", "LKT_007"
            })
            {
                Assert.IsNotNull(StarterCardLibrary.Get(id), id);
            }
        }
    }
}
