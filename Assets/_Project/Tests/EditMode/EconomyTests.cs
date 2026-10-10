using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบการย่อย/คราฟการ์ด และ Achievement
    /// </summary>
    public class EconomyTests
    {
        private const string Common = "STR_001";

        private static PlayerProfile NewProfile()
        {
            return new PlayerProfile(new SaveData());
        }

        [Test]
        public void Values_FollowRarity()
        {
            Assert.AreEqual(5, CardEconomy.DisenchantValue(CardRarity.Common));
            Assert.AreEqual(400, CardEconomy.DisenchantValue(CardRarity.Legendary));
            Assert.AreEqual(40, CardEconomy.CraftCost(CardRarity.Common));
            Assert.AreEqual(1600, CardEconomy.CraftCost(CardRarity.Legendary));
            Assert.AreEqual(0, CardEconomy.CraftCost(CardRarity.Secret));
        }

        [Test]
        public void Disenchant_RemovesCardAndGivesDust()
        {
            PlayerProfile profile = NewProfile();

            profile.AddCard(Common, 3);

            string message;

            Assert.IsTrue(CardEconomy.Disenchant(
                profile, CardRegistry.Get(Common), 1, out message));

            Assert.AreEqual(2, profile.GetOwnedCount(Common));
            Assert.AreEqual(5, profile.Dust);
            Assert.AreEqual(1, profile.Data.StatDisenchanted);
        }

        [Test]
        public void Disenchant_KeepsCopiesUsedInDecks()
        {
            PlayerProfile profile = NewProfile();

            profile.AddCard(Common, 2);

            DeckList deck = profile.AddDeck("Test");

            deck.CardIDs.Add(Common);
            deck.CardIDs.Add(Common);

            string message;

            Assert.AreEqual(0, CardEconomy.DisenchantableCopies(
                profile, CardRegistry.Get(Common)));

            Assert.IsFalse(CardEconomy.Disenchant(
                profile, CardRegistry.Get(Common), 1, out message));

            Assert.AreEqual(2, profile.GetOwnedCount(Common));
        }

        [Test]
        public void Craft_SpendsDustAndAddsCard()
        {
            PlayerProfile profile = NewProfile();

            profile.AddDust(40);

            string message;

            Assert.IsTrue(CardEconomy.Craft(
                profile, CardRegistry.Get(Common), out message));

            Assert.AreEqual(1, profile.GetOwnedCount(Common));
            Assert.AreEqual(0, profile.Dust);
            Assert.AreEqual(1, profile.Data.StatCrafted);
        }

        [Test]
        public void Craft_FailsWithoutDustOrAtMaxCopies()
        {
            PlayerProfile profile = NewProfile();

            string message;

            Assert.IsFalse(CardEconomy.Craft(
                profile, CardRegistry.Get(Common), out message));

            profile.AddDust(1000);
            profile.AddCard(Common, 2);

            Assert.IsFalse(CardEconomy.Craft(
                profile, CardRegistry.Get(Common), out message));

            Assert.AreEqual(2, profile.GetOwnedCount(Common));
        }

        [Test]
        public void SecretCard_IsNotCollectibleOrCraftable()
        {
            CardData secret = CardRegistry.Get(StarterCardLibrary.SecretId);

            Assert.IsNotNull(secret);
            Assert.IsFalse(CardRegistry.IsCollectible(secret));

            PlayerProfile profile = NewProfile();

            profile.AddDust(100000);

            string message;

            Assert.IsFalse(CardEconomy.Craft(profile, secret, out message));
        }

        [Test]
        public void ExtraCopies_CountsOnlyAboveDeckLimit()
        {
            PlayerProfile profile = NewProfile();

            profile.AddCard(Common, 5);

            Assert.AreEqual(3, CardEconomy.ExtraCopies(
                profile, CardRegistry.Get(Common)));

            int cards;

            int dust = CardEconomy.DisenchantAllExtras(profile, out cards);

            Assert.AreEqual(3, cards);
            Assert.AreEqual(15, dust);
            Assert.AreEqual(2, profile.GetOwnedCount(Common));
        }

        [Test]
        public void Achievement_UnlocksOnceAndGivesReward()
        {
            PlayerProfile profile = NewProfile();

            profile.Data.StatWins = 1;

            var first = AchievementService.Check(profile);

            Assert.IsTrue(first.Exists(a => a.Id == "first_win"));
            Assert.AreEqual(50, profile.Gold);

            var second = AchievementService.Check(profile);

            Assert.IsFalse(second.Exists(a => a.Id == "first_win"));
            Assert.AreEqual(50, profile.Gold);
        }

        [Test]
        public void Achievement_SecretIsHiddenUntilFound()
        {
            PlayerProfile profile = NewProfile();

            AchievementDef secret = null;

            foreach (AchievementDef def in AchievementService.All)
            {
                if (def.Id == "secret")
                    secret = def;
            }

            Assert.IsNotNull(secret);
            Assert.IsTrue(secret.Hidden);
            Assert.IsFalse(AchievementService.IsUnlocked(profile, secret));

            profile.Data.StatSecrets = 1;

            AchievementService.Check(profile);

            Assert.IsTrue(AchievementService.IsUnlocked(profile, secret));
            Assert.AreEqual(300, profile.Dust);
        }
    }
}
