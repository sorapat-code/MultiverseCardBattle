using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ทดสอบการย่อย/คราฟการ์ด และ Achievement
    /// </summary>
    public class EconomyTests
    {
        private const string Common = "SUP_001";

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
            profile.AddCard(Common, 3);

            Assert.IsFalse(CardEconomy.Craft(
                profile, CardRegistry.Get(Common), out message));

            Assert.AreEqual(3, profile.GetOwnedCount(Common));
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

            Assert.AreEqual(2, CardEconomy.ExtraCopies(
                profile, CardRegistry.Get(Common)));

            int cards;

            int dust = CardEconomy.DisenchantAllExtras(profile, out cards);

            Assert.AreEqual(2, cards);
            Assert.AreEqual(10, dust);
            Assert.AreEqual(3, profile.GetOwnedCount(Common));
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

        [Test]
        public void MaxCopies_IsThree_AndOneForUnique()
        {
            Assert.AreEqual(3, DeckRules.MaxCopies(CardRegistry.Get(Common)));
            Assert.AreEqual(3, DeckRules.MaxCopies(CardRegistry.Get("SUP_012")));

            CardData secret = CardRegistry.Get(StarterCardLibrary.SecretId);

            Assert.AreEqual(1, DeckRules.MaxCopies(secret));
            Assert.AreNotEqual(0, (int)(secret.Keywords & Keyword.Unique));
        }

        [Test]
        public void SecretCard_VisibleOnlyAfterUnlock_AndDeckable()
        {
            PlayerProfile profile = NewProfile();

            CardData secret = CardRegistry.Get(StarterCardLibrary.SecretId);

            Assert.IsTrue(CardRegistry.IsDeckable(secret));
            Assert.IsFalse(CardRegistry.IsVisibleInCollection(secret, profile));

            var lines = CampaignService.UnlockSecrets(
                profile, new[] { StarterCardLibrary.SecretId });

            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual(1, profile.GetOwnedCount(StarterCardLibrary.SecretId));
            Assert.IsTrue(CardRegistry.IsVisibleInCollection(secret, profile));

            // ปลดล็อกซ้ำไม่ได้เพิ่มใบ
            Assert.AreEqual(0, CampaignService.UnlockSecrets(
                profile, new[] { StarterCardLibrary.SecretId }).Count);
            Assert.AreEqual(1, profile.GetOwnedCount(StarterCardLibrary.SecretId));
        }

        [Test]
        public void SecretCard_CannotBeDisenchantedOrCrafted_ButCanBeInDeck()
        {
            PlayerProfile profile = NewProfile();

            profile.AddCard(StarterCardLibrary.SecretId, 1);
            profile.AddDust(100000);

            CardData secret = CardRegistry.Get(StarterCardLibrary.SecretId);

            string message;

            Assert.IsFalse(CardEconomy.Disenchant(profile, secret, 1, out message));
            Assert.IsFalse(CardEconomy.Craft(profile, secret, out message));

            DeckList deck = new DeckList();

            deck.CardIDs.Add(StarterCardLibrary.SecretId);
            deck.CardIDs.Add(StarterCardLibrary.SecretId);

            var problems = DeckRules.Validate(deck, profile);

            Assert.IsTrue(problems.Exists(p => p.Contains("max 1")));
            Assert.IsFalse(problems.Exists(p => p.Contains("cannot be put")));
        }
    }
}
