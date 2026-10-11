using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Screens;
using MultiverseCardBattle.Targeting;

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
        public void SkillText_ListsKeywordsAndAbilityText()
        {
            CardData march = CardRegistry.Get("SUP_008");

            string text = GameGlossary.SkillTextFor(march, null);

            StringAssert.Contains("Taunt", text);
            StringAssert.Contains("Battlecry", text);
            StringAssert.Contains("Barrier", text);
            StringAssert.DoesNotContain("<", text);
        }

        [Test]
        public void SkillTerms_CoverEveryWordUnderlinedInTheSkillText()
        {
            CardData march = CardRegistry.Get("SUP_008");

            List<string[]> terms = GameGlossary.TermsFor(march, null);

            bool taunt = false;
            bool battlecry = false;
            bool barrier = false;

            foreach (string[] term in terms)
            {
                Assert.IsNotEmpty(term[1], term[0]);

                taunt |= term[0] == "Taunt";
                battlecry |= term[0] == "Battlecry";
                barrier |= term[0] == "Barrier";
            }

            Assert.IsTrue(taunt && battlecry && barrier);
        }

        [Test]
        public void SkillText_ShowsStatusesOfAnInstance()
        {
            CardInstance card = new CardInstance(CardRegistry.Get("SUP_009"));

            card.AddStatus(StatusType.Burn, 2, 2);

            StringAssert.Contains("Status: Burn", GameGlossary.SkillTextFor(card.Data, card));
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
            CardData scroll = ScriptableObject.CreateInstance<CardData>();

            scroll.CardID = "TEST_DISCOVER";
            scroll.CardName = "Test Discover";
            scroll.Type = CardType.Spell;

            CardActionData action = new CardActionData();

            action.Trigger = TriggerType.Play;
            action.Target = TargetRule.AllyLeader;
            action.Selection = TargetSelection.All;

            EffectData effect = new EffectData();

            effect.Type = EffectType.Discover;
            effect.Value = 3;

            action.Effects = new List<EffectData> { effect };
            scroll.Actions = new List<CardActionData> { action };

            string hints = GameGlossary.HintsFor(scroll, null);

            Object.DestroyImmediate(scroll);

            StringAssert.Contains("Discover", hints);
            StringAssert.Contains("3", hints);
        }

        [Test]
        public void Hints_ForImmuneCard_ShowTheAmount()
        {
            CardData immune = ScriptableObject.CreateInstance<CardData>();

            immune.CardID = "TEST_IMMUNE";
            immune.CardName = "Test Immune";
            immune.Type = CardType.Unit;
            immune.Keywords = Keyword.Immune;

            string hints = GameGlossary.HintsFor(immune, null);

            StringAssert.Contains("Immune", hints);
            StringAssert.Contains(immune.EffectiveImmuneAmount.ToString(), hints);

            StringAssert.Contains(
                "Immune " + immune.EffectiveImmuneAmount,
                CardTextGenerator.Describe(immune));

            Object.DestroyImmediate(immune);
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
