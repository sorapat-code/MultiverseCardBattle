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

            for (int i = 0; i < 3; i++)
                deck.CardIDs.Add("STR_001");

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
    }
}
