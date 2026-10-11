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
    /// <summary>ทดสอบ Artorias the Abysswalker และเงื่อนไขปลดล็อก</summary>
    public class ArtoriasTests
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

        private CardInstance Place(BattlePlayer owner, string id)
        {
            CardInstance card = new CardInstance(StarterCardLibrary.Get(id));

            card.OwnerID = owner.PlayerID;

            battle.SummonUnit(card, owner);

            card.SummonedOnTurn = -100;

            return card;
        }

        [Test]
        public void Summon_DamagesEnemiesAndHealsPerHit()
        {
            Place(P2, "SUP_015");
            Place(P2, "SUP_015");

            P1.LeaderHealth = 10;

            CardInstance artorias = new CardInstance(
                StarterCardLibrary.Get(StarterCardLibrary.ArtoriasId));

            artorias.OwnerID = P1.PlayerID;
            P1.Hand.Add(artorias);

            Assert.IsTrue(battle.Submit(new PlayCardCommand(P1, artorias)).Success);

            // 2 ยูนิตศัตรูโดนจริง: Leader ฟื้น 2 x 2 = 4
            // (Lifesteal ของ Artorias ไม่เกี่ยวกับ Summon นี้)
            Assert.GreaterOrEqual(P1.LeaderHealth, 14);

            foreach (CardInstance enemy in P2.Field)
                Assert.AreEqual(2, enemy.CurrentHealth);
        }

        [Test]
        public void LastGuardian_SurvivesOnceAndLosesTaunt()
        {
            CardInstance artorias = Place(P1, StarterCardLibrary.ArtoriasId);

            Assert.IsTrue(artorias.HasKeyword(Keyword.Taunt));

            battle.KillUnit(artorias);

            Assert.AreEqual(CardZone.Field, artorias.Zone);
            Assert.AreEqual(1, artorias.CurrentHealth);
            Assert.IsFalse(artorias.HasKeyword(Keyword.Taunt));

            // ครั้งที่สองตายจริง
            battle.KillUnit(artorias);

            Assert.IsFalse(P1.Field.Contains(artorias));
        }

        [Test]
        public void Abysswalker_GainsAttackWhenDamagedByAttack()
        {
            CardInstance artorias = Place(P1, StarterCardLibrary.ArtoriasId);
            CardInstance enemy = Place(P2, "SUP_026");

            battle.TurnManager.EndTurn();

            Assert.IsTrue(battle.Submit(
                new AttackCommand(P2, enemy, artorias, null)).Success);

            Assert.AreEqual(7, artorias.CurrentAttack);
        }

        [Test]
        public void Unlock_NeedsAllConditionsAndAWin()
        {
            BattleStats stats = battle.Stats;
            int h = P1.PlayerID;

            Assert.IsFalse(SecretCardService.IsArtoriasEarned(stats, h));

            for (int i = 0; i < 4; i++) stats.AddCombatWin(h);
            for (int i = 0; i < 3; i++) stats.AddUnitDeath(h);

            Assert.IsTrue(SecretCardService.IsArtoriasEarned(stats, h));

            stats.AddInstantDestroy(h);
            Assert.IsFalse(SecretCardService.IsArtoriasEarned(stats, h));
        }

        [Test]
        public void Unlock_FailsIfLeaderDropsBelowTen()
        {
            BattleStats stats = battle.Stats;
            int h = P1.PlayerID;

            for (int i = 0; i < 4; i++) stats.AddCombatWin(h);
            for (int i = 0; i < 3; i++) stats.AddUnitDeath(h);

            P1.LeaderHealth = 9;
            stats.NoteLeaderHealth(P1);

            Assert.IsFalse(SecretCardService.IsArtoriasEarned(stats, h));
        }
    }
}
