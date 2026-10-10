using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Screens;

namespace MultiverseCardBattle.Tests
{
    public class GlossaryTests
    {
        [Test]
        public void FullGlossary_ListsGameTermsAndDiscover()
        {
            string text = GameGlossary.Build();

            StringAssert.Contains("Discover", text);
            StringAssert.Contains("Graveyard", text);
            StringAssert.Contains("Energy", text);
            StringAssert.Contains("Coin flip", text);
            StringAssert.Contains("Counterattack", text);
        }

        [Test]
        public void FullGlossary_ListsEveryKeywordAndStatus()
        {
            string text = GameGlossary.Build();

            foreach (Keyword keyword in System.Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                StringAssert.Contains(
                    CardTextGenerator.KeywordName(keyword), text);
            }
        }

        [Test]
        public void Hints_ForDiscoverCard_ExplainDiscover()
        {
            CardData scroll = StarterCardLibrary.Get("SPL_009");

            string hints = GameGlossary.HintsFor(scroll, null);

            StringAssert.Contains("Discover", hints);
            StringAssert.Contains("3", hints);
        }

        [Test]
        public void Hints_ForImmuneCard_ShowTheAmount()
        {
            CardData immune = null;

            foreach (CardData card in StarterCardLibrary.All)
            {
                if ((card.Keywords & Keyword.Immune) != 0)
                    immune = card;
            }

            Assert.IsNotNull(immune);

            string hints = GameGlossary.HintsFor(immune, null);

            StringAssert.Contains("Immune", hints);
            StringAssert.Contains(immune.EffectiveImmuneAmount.ToString(), hints);

            StringAssert.Contains(
                "Immune " + immune.EffectiveImmuneAmount,
                CardTextGenerator.Describe(immune));
        }

        [Test]
        public void Pretty_SplitsCamelCase()
        {
            Assert.AreEqual("Honkai Star Rail", CardFilterBar.Pretty("HonkaiStarRail"));
            Assert.AreEqual("Naruto", CardFilterBar.Pretty("Naruto"));
        }

        [Test]
        public void NewDeckName_UsesSmallestFreeNumber()
        {
            PlayerProfile profile = new PlayerProfile(new SaveData());

            DeckList first = profile.AddDeck(null);

            Assert.AreEqual("Deck 1", first.Name);

            DeckList second = profile.AddDeck(null);

            Assert.AreEqual("Deck 2", second.Name);

            profile.RemoveDeck(first);

            Assert.AreEqual("Deck 1", profile.AddDeck(null).Name);
        }
    }
}
