using System.Collections.Generic;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Decks;

namespace MultiverseCardBattle.Tests
{
    public class DataTests
    {
        [Test]
        public void StarterCards_HaveUniqueIds()
        {
            HashSet<string> ids = new HashSet<string>();

            foreach (CardData card in StarterCardLibrary.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(card.CardID));
                Assert.IsTrue(ids.Add(card.CardID), "Duplicate id " + card.CardID);
            }
        }

        [Test]
        public void NewProfile_HasValidActiveDeck()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            List<string> problems =
                DeckRules.Validate(profile.ActiveDeck, profile);

            Assert.IsEmpty(problems, string.Join("; ", problems));
        }

        [Test]
        public void CampaignStages_ReferenceExistingCards()
        {
            HashSet<string> stageIds = new HashSet<string>();

            foreach (CampaignStage stage in CampaignLibrary.Stages)
            {
                Assert.IsTrue(stageIds.Add(stage.Id), "Duplicate stage " + stage.Id);
            }

            foreach (CampaignStage stage in CampaignLibrary.Stages)
            {
                Assert.GreaterOrEqual(
                    stage.EnemyDeckIds.Count, DeckRules.MinDeckSize,
                    stage.Name + " enemy deck is too small");

                foreach (string id in stage.EnemyDeckIds)
                    Assert.IsNotNull(CardRegistry.Get(id), stage.Id + ": " + id);

                if (stage.RewardCardIds != null)
                {
                    foreach (string id in stage.RewardCardIds)
                        Assert.IsNotNull(CardRegistry.Get(id), stage.Id + " reward " + id);
                }

                if (!string.IsNullOrEmpty(stage.RequiresStageId))
                    Assert.IsTrue(stageIds.Contains(stage.RequiresStageId));
            }
        }

        [Test]
        public void DeckRules_RejectTooManyCopiesAndSmallDecks()
        {
            DeckList deck = new DeckList();

            for (int i = 0; i < 4; i++)
                deck.CardIDs.Add("SUP_001");

            List<string> problems = DeckRules.Validate(deck, null);

            Assert.IsTrue(problems.Count >= 2);
        }

        [Test]
        public void DeckRules_RequireOwnership()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            DeckList deck = new DeckList();

            // การ์ดที่ผู้เล่นใหม่ไม่มี
            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) &&
                    profile.GetOwnedCount(card.CardID) == 0)
                {
                    deck.CardIDs.Add(card.CardID);
                    break;
                }
            }

            List<string> problems = DeckRules.Validate(deck, profile);

            Assert.IsTrue(problems.Exists(p => p.Contains("own")));
        }

        [Test]
        public void Library_HasFortyCollectibleCardsFromManyUniverses()
        {
            int count = 0;
            HashSet<Universe> universes = new HashSet<Universe>();

            foreach (CardData card in CardRegistry.All)
            {
                if (!CardRegistry.IsCollectible(card))
                    continue;

                count++;
                universes.Add(card.Universe);
            }

            Assert.GreaterOrEqual(count, 40);
            Assert.GreaterOrEqual(universes.Count, 8);
        }

        [Test]
        public void EveryCollectibleCard_CanBeObtained()
        {
            HashSet<string> obtainable =
                new HashSet<string>(StarterDecks.StarterCollectionIds());

            foreach (CampaignStage stage in CampaignLibrary.Stages)
            {
                if (stage.RewardCardIds == null)
                    continue;

                foreach (string id in stage.RewardCardIds)
                    obtainable.Add(id);
            }

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card))
                    Assert.IsTrue(obtainable.Contains(card.CardID),
                        card.CardID + " cannot be obtained");
            }
        }

        [Test]
        public void Pack_HasFiveCardsWithAtLeastOneRarePlus()
        {
            System.Random rng = new System.Random(12345);

            for (int i = 0; i < 200; i++)
            {
                List<CardData> cards = PackService.RollCards(rng);

                Assert.AreEqual(PackService.CardsPerPack, cards.Count);

                bool rarePlus = false;

                foreach (CardData card in cards)
                {
                    Assert.IsTrue(CardRegistry.IsCollectible(card));

                    if (card.Rarity != CardRarity.Common)
                        rarePlus = true;
                }

                Assert.IsTrue(rarePlus, "Pack " + i + " had no Rare or better");
            }
        }

        [Test]
        public void BuyPack_SpendsGoldAndAddsCards()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            int goldBefore = profile.Gold;

            int copiesBefore = 0;

            foreach (OwnedCardEntry entry in profile.Owned)
                copiesBefore += entry.Count;

            string error;

            List<PackPull> pulls =
                PackService.Buy(profile, new System.Random(1), out error);

            Assert.IsNotNull(pulls, error);
            Assert.AreEqual(PackService.CardsPerPack, pulls.Count);
            Assert.AreEqual(goldBefore - PackService.PackPrice, profile.Gold);

            int copiesAfter = 0;

            foreach (OwnedCardEntry entry in profile.Owned)
                copiesAfter += entry.Count;

            Assert.AreEqual(copiesBefore + PackService.CardsPerPack, copiesAfter);
        }

        [Test]
        public void BuyPack_FailsWithoutEnoughGold()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            profile.TrySpendGold(profile.Gold);

            string error;

            List<PackPull> pulls =
                PackService.Buy(profile, new System.Random(1), out error);

            Assert.IsNull(pulls);
            Assert.IsNotNull(error);
            Assert.AreEqual(0, profile.Gold);
        }

        [Test]
        public void Gold_RejectsOverspendAndNegativeValues()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            int gold = profile.Gold;

            Assert.IsFalse(profile.TrySpendGold(gold + 1));
            Assert.AreEqual(gold, profile.Gold);

            profile.AddGold(-50);
            Assert.AreEqual(gold, profile.Gold);

            profile.AddGold(50);
            Assert.AreEqual(gold + 50, profile.Gold);
        }

        [Test]
        public void RemoveDeck_KeepsSameActiveDeck()
        {
            PlayerProfile profile = PlayerProfile.CreateNew();

            DeckList second = profile.AddDeck("Second");
            DeckList third = profile.AddDeck("Third");

            profile.SetActiveDeck(2);

            Assert.AreSame(third, profile.ActiveDeck);

            profile.RemoveDeck(profile.Decks[0]);

            Assert.AreSame(third, profile.ActiveDeck);
            Assert.IsNotNull(second);
        }

        [Test]
        public void Keywords_AddRemoveAndSilence()
        {
            CardData data = UnityEngine.ScriptableObject.CreateInstance<CardData>();

            data.CardID = "T";
            data.CardName = "T";
            data.Health = 3;
            data.Keywords = Keyword.Taunt | Keyword.Shield;

            CardInstance card = new CardInstance(data);

            Assert.IsTrue(card.HasKeyword(Keyword.Taunt));

            card.RemoveKeyword(Keyword.Taunt);
            Assert.IsFalse(card.HasKeyword(Keyword.Taunt));
            Assert.IsTrue(card.HasKeyword(Keyword.Shield));

            card.AddStatus(StatusType.Silence, 0, StatusEffect.Permanent);
            Assert.IsFalse(card.HasKeyword(Keyword.Shield));
            Assert.IsTrue(card.IsSilenced);

            UnityEngine.Object.DestroyImmediate(data);
        }

        [Test]
        public void Deck_AllowsOnlyOneUniqueCard()
        {
            DeckList deck = new DeckList();

            deck.CardIDs.Add("EVT_001");
            deck.CardIDs.Add("EVT_002");

            List<string> problems = DeckRules.Validate(deck, null);

            Assert.IsTrue(problems.Exists(p => p.Contains("Unique")));
            Assert.IsNotNull(DeckRules.FindUnique(deck));

            deck.CardIDs.Remove("EVT_002");

            problems = DeckRules.Validate(deck, null);

            Assert.IsFalse(problems.Exists(p => p.Contains("Unique")));
        }
    }
}
