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
    /// <summary>ทดสอบระบบอุปกรณ์ (Unit gear / Leader gear) และ Backstab</summary>
    public class EquipmentTests
    {
        private GameObject host;
        private BattleManager battle;
        private CardData filler;
        private readonly List<CardData> made = new List<CardData>();

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

            foreach (CardData data in made)
                Object.DestroyImmediate(data);

            made.Clear();

            BattleSetup.Reset();
        }

        // ---------- การ์ดทดสอบ (ไม่พึ่งการ์ดในคลัง) ----------

        private CardData MakeCard(
            string id, CardType type, int attack, int health, Keyword keywords)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            data.CardID = id;
            data.CardName = id;
            data.Type = type;
            data.Cost = 1;
            data.Attack = attack;
            data.Health = health;
            data.Keywords = keywords;
            data.Actions = new List<CardActionData>();

            made.Add(data);

            return data;
        }

        private static CardActionData Act(
            TriggerType trigger, TargetRule rule, TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = trigger;
            action.Target = rule;
            action.Selection = selection;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private static EffectData Fx(EffectType type, int value = 0)
        {
            EffectData effect = new EffectData();

            effect.Type = type;
            effect.Value = value;

            return effect;
        }

        // อุปกรณ์สวมยูนิต +4/+2 Shield Lifesteal
        private CardData MakeUnitGear()
        {
            CardData gear = MakeCard("T_GEAR", CardType.Equipment, 0, 0, Keyword.None);

            gear.Actions.Add(Act(TriggerType.Play, TargetRule.AllyUnit,
                TargetSelection.Chosen, Fx(EffectType.Equip)));

            gear.GearAttack = 4;
            gear.GearHealth = 2;
            gear.GearKeywords = Keyword.Shield | Keyword.Lifesteal;

            return gear;
        }

        // อุปกรณ์ Leader อยู่ 4 เทิร์น ฮีลตอนเริ่มเทิร์น 2
        private CardData MakeLeaderGear()
        {
            CardData gear = MakeCard("T_LGEAR", CardType.Equipment, 0, 0, Keyword.None);

            gear.Actions.Add(Act(TriggerType.Play, TargetRule.AllyLeader,
                TargetSelection.All, Fx(EffectType.EquipLeader)));

            gear.Actions.Add(Act(TriggerType.TurnStart, TargetRule.AllyLeader,
                TargetSelection.All, Fx(EffectType.Heal, 2)));

            gear.GearDurability = 4;

            return gear;
        }

        private CardInstance Place(BattlePlayer owner, CardData data)
        {
            CardInstance card = new CardInstance(data);

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        private CardInstance InHand(CardData data)
        {
            CardInstance card = new CardInstance(data);

            card.OwnerID = P1.PlayerID;
            P1.Hand.Add(card);

            return card;
        }

        [Test]
        public void UnitGear_AddsStatsAndKeywordsToChosenUnit()
        {
            CardInstance unit = Place(P1, MakeCard("T_UNIT", CardType.Unit, 2, 2, Keyword.None));

            CardInstance gear = InHand(MakeUnitGear()); // +4/+2 Shield Lifesteal

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
            CardInstance gear = InHand(MakeUnitGear());

            Assert.IsFalse(
                BattleRules.ValidatePlayable(battle, P1, gear).Success);
        }

        [Test]
        public void LeaderGear_StaysAndTicksDownThenExpires()
        {
            CardInstance gear = InHand(MakeLeaderGear()); // 4 turns

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
            CardInstance gear = InHand(MakeLeaderGear());

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
            CardInstance source = Place(P1,
                MakeCard("T_STAB", CardType.Unit, 2, 2, Keyword.Backstab));
            CardInstance big = Place(P2,
                MakeCard("T_BIG", CardType.Unit, 4, 7, Keyword.Taunt));

            Assert.IsTrue(source.HasKeyword(Keyword.Backstab));

            battle.DamageUnit(source, big, 1);
            battle.Resolve();

            Assert.AreNotEqual(CardZone.Field, big.Zone);
        }
    }
}
