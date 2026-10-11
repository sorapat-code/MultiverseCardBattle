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
    /// <summary>ทดสอบ Ainz Ooal Gown และเงื่อนไขปลดล็อก The Throne of Nazarick</summary>
    public class AinzTests
    {
        private GameObject host;
        private BattleManager battle;
        private CardData filler;
        private CardData spell;

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

            spell = ScriptableObject.CreateInstance<CardData>();

            spell.CardID = "TEST_SPELL";
            spell.CardName = "TEST_SPELL";
            spell.Type = CardType.Spell;
            spell.Cost = 3;

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
            Object.DestroyImmediate(spell);

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

        private CardInstance SpellInHand()
        {
            CardInstance card = new CardInstance(spell);

            card.OwnerID = P1.PlayerID;

            P1.Hand.Add(card);

            return card;
        }

        [Test]
        public void Play_AppliesDoomAndDestroysAtNextTurnStart()
        {
            CardInstance enemy = Place(P2, "STR_005");

            CardInstance ainz = new CardInstance(
                StarterCardLibrary.Get(StarterCardLibrary.AinzId));

            ainz.OwnerID = P1.PlayerID;
            P1.Hand.Add(ainz);

            List<TargetCandidate> options =
                BattleRules.GetPlayTargets(battle, P1, ainz);

            Assert.AreEqual(1, options.Count);

            Assert.IsTrue(battle.Submit(
                new PlayCardCommand(P1, ainz, options[0])).Success);

            Assert.IsTrue(enemy.HasStatus(StatusType.Doom));
            Assert.AreEqual(CardZone.Field, enemy.Zone);

            battle.TurnManager.EndTurn();
            Assert.AreEqual(CardZone.Field, enemy.Zone);

            battle.TurnManager.EndTurn();

            Assert.AreEqual(CardZone.Graveyard, enemy.Zone);
        }

        [Test]
        public void SupremeMagicCaster_DiscountsFirstTwoSpells()
        {
            Place(P1, StarterCardLibrary.AinzId);

            CardInstance a = SpellInHand();
            CardInstance b = SpellInHand();
            CardInstance c = SpellInHand();

            battle.Resolve();

            Assert.AreEqual(1, a.CurrentCost);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, a)).Success);

            Assert.AreEqual(2, b.CurrentCost);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, b)).Success);

            Assert.AreEqual(3, c.CurrentCost);
            Assert.AreEqual(2, battle.Stats.SpellsPlayed[P1.PlayerID]);
        }

        [Test]
        public void UndeadOverlord_SurvivesDamageOnceButNotDestroy()
        {
            CardInstance ainz = Place(P1, StarterCardLibrary.AinzId);

            battle.DamageUnit(null, ainz, 99);
            battle.Resolve();

            Assert.AreEqual(CardZone.Field, ainz.Zone);
            Assert.AreEqual(1, ainz.CurrentHealth);
            Assert.IsTrue(ainz.HasKeyword(Keyword.Unstoppable));

            battle.DamageUnit(null, ainz, 99);
            battle.Resolve();

            Assert.AreNotEqual(CardZone.Field, ainz.Zone);
        }

        [Test]
        public void UndeadOverlord_DirectDestroyStillKills()
        {
            CardInstance ainz = Place(P1, StarterCardLibrary.AinzId);

            battle.KillUnit(ainz);

            Assert.AreNotEqual(CardZone.Field, ainz.Zone);
        }

        [Test]
        public void DarkWisdom_GivesEnergyUpToThreePerTurn()
        {
            Place(P1, StarterCardLibrary.AinzId);

            CardInstance source = SpellInHand();

            P1.CurrentEnergy = 0;

            for (int i = 0; i < 4; i++)
            {
                CardInstance enemy = Place(P2, "STR_001");

                battle.ActiveEffectSource = source;
                battle.ActiveEffectPlayer = P1;

                battle.DamageUnit(source, enemy, 99);

                battle.ActiveEffectSource = null;
                battle.ActiveEffectPlayer = null;
            }

            battle.Resolve();

            Assert.AreEqual(3, P1.CurrentEnergy);
            Assert.AreEqual(4, battle.Stats.EffectKills[P1.PlayerID]);
        }

        [Test]
        public void CombatKills_DoNotCountAsEffectKills()
        {
            Place(P1, StarterCardLibrary.AinzId);

            CardInstance enemy = Place(P2, "STR_001");

            battle.DamageUnit(null, enemy, 99);
            battle.Resolve();

            Assert.AreEqual(0, battle.Stats.EffectKills[P1.PlayerID]);
        }

        [Test]
        public void GraspHeart_KillSplashesTwoOtherEnemies()
        {
            CardInstance ainz = Place(P1, StarterCardLibrary.AinzId);
            CardInstance weak = Place(P2, "STR_001");
            CardInstance other1 = Place(P2, "STR_005");
            CardInstance other2 = Place(P2, "STR_005");

            battle.OnTurnStartPassives(P1);

            Assert.IsTrue(battle.HasPendingTarget);

            Assert.IsTrue(battle.ResolvePendingTarget(weak));

            Assert.AreNotEqual(CardZone.Field, weak.Zone);
            Assert.AreEqual(4, other1.CurrentHealth);
            Assert.AreEqual(4, other2.CurrentHealth);
            Assert.IsFalse(battle.HasPendingTarget);
            Assert.AreEqual(CardZone.Field, ainz.Zone);
        }

        [Test]
        public void GraspHeart_NonLethalDoesNotSplash()
        {
            Place(P1, StarterCardLibrary.AinzId);

            CardInstance big = Place(P2, "STR_005");
            CardInstance other = Place(P2, "STR_005");

            battle.OnTurnStartPassives(P1);

            Assert.IsTrue(battle.ResolvePendingTarget(big));

            Assert.AreEqual(3, big.CurrentHealth);
            Assert.AreEqual(7, other.CurrentHealth);
        }

        [Test]
        public void Unlock_NeedsSpellsKillsAndHealthAndWin()
        {
            BattleStats stats = battle.Stats;
            int h = P1.PlayerID;

            P1.LeaderHealth = 20;

            Assert.IsFalse(SecretCardService.IsAinzEarned(stats, h, P1));

            for (int i = 0; i < 5; i++) stats.AddSpellPlayed(h);
            for (int i = 0; i < 4; i++) stats.AddEffectKill(h);

            Assert.IsTrue(SecretCardService.IsAinzEarned(stats, h, P1));

            P1.LeaderHealth = 14;

            Assert.IsFalse(SecretCardService.IsAinzEarned(stats, h, P1));
        }
    }
}
