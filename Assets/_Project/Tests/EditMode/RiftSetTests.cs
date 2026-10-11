using System;
using System.Collections.Generic;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ทดสอบ Rift Pack: จำนวน/สัดส่วนการ์ด ซอง และ Card Pool</summary>
    public class RiftSetTests
    {
        [Test]
        public void RiftPool_Has50Cards_30Units_12Spells_8Equipment()
        {
            List<CardData> pool = PackService.PoolOf(CardSet.Rift);

            Assert.AreEqual(50, pool.Count);

            int units = 0, spells = 0, gear = 0;

            foreach (CardData card in pool)
            {
                if (card.Type == CardType.Unit) units++;
                else if (card.Type == CardType.Spell) spells++;
                else if (card.Type == CardType.Equipment) gear++;
            }

            Assert.AreEqual(30, units);
            Assert.AreEqual(12, spells);
            Assert.AreEqual(8, gear);
        }

        [Test]
        public void RiftPool_RarityMix_IsAsDesigned()
        {
            int c = 0, r = 0, e = 0, l = 0;

            foreach (CardData card in PackService.PoolOf(CardSet.Rift))
            {
                switch (card.Rarity)
                {
                    case CardRarity.Common: c++; break;
                    case CardRarity.Rare: r++; break;
                    case CardRarity.Epic: e++; break;
                    case CardRarity.Legendary: l++; break;
                }
            }

            Assert.AreEqual(18, c);
            Assert.AreEqual(17, r);
            Assert.AreEqual(10, e);
            Assert.AreEqual(5, l);
        }

        [Test]
        public void RiftPool_TokensAreNotInThePool()
        {
            foreach (CardData card in PackService.PoolOf(CardSet.Rift))
                Assert.AreNotEqual(CardSource.Token, card.Source, card.CardID);
        }

        [Test]
        public void CorePool_DoesNotContainRiftCards()
        {
            foreach (CardData card in PackService.PoolOf(CardSet.Core))
                Assert.AreEqual(CardSet.Core, card.Set, card.CardID);
        }

        [Test]
        public void RiftPack_RollsOnlyRiftCards()
        {
            Random rng = new Random(7);

            for (int i = 0; i < 50; i++)
            {
                List<CardData> cards = PackService.RollCards(rng, CardSet.Rift);

                Assert.AreEqual(PackService.CardsPerPack, cards.Count);

                foreach (CardData card in cards)
                    Assert.AreEqual(CardSet.Rift, card.Set, card.CardID);
            }
        }

        [Test]
        public void BuyRiftPack_CostsRiftPrice()
        {
            PlayerProfile profile = new PlayerProfile(new SaveData());

            profile.AddGold(1000);

            string error;

            List<PackPull> pulls = PackService.Buy(
                profile, new Random(3), CardSet.Rift, out error);

            Assert.IsNotNull(pulls, error);
            Assert.AreEqual(1000 - CardSetInfo.PackPrice(CardSet.Rift), profile.Gold);
        }

        [Test]
        public void RiftCards_FormsAreTransformAndReferenceExistingTokens()
        {
            string[] formCards =
                { "RFT_022", "RFT_023", "RFT_024", "RFT_025", "RFT_027" };

            foreach (string id in formCards)
            {
                CardData card = CardRegistry.Get(id);

                Assert.IsNotNull(card, id);

                bool hasTransform = false;

                foreach (var action in card.Actions)
                {
                    foreach (var effect in action.Effects)
                    {
                        if (effect.Type == MultiverseCardBattle.Effects.EffectType.Transform)
                        {
                            hasTransform = true;
                            Assert.IsNotNull(effect.Card, id);
                        }
                    }
                }

                Assert.IsTrue(hasTransform, id + " should transform");
            }
        }

        [Test]
        public void Equipment_AllHaveABonusOrDurability()
        {
            foreach (CardData card in PackService.PoolOf(CardSet.Rift))
            {
                if (card.Type != CardType.Equipment)
                    continue;

                bool bonus =
                    card.GearAttack != 0 || card.GearHealth != 0 ||
                    card.GearKeywords != Keyword.None;

                Assert.IsTrue(bonus || card.GearDurability > 0, card.CardID);
                Assert.IsNotEmpty(card.Description, card.CardID);
            }
        }

        [Test]
        public void SecretAndProtectedCards_StayOutOfEveryPool()
        {
            foreach (CardSet set in new[] { CardSet.Core, CardSet.Rift })
            {
                foreach (CardData card in PackService.PoolOf(set))
                {
                    Assert.AreNotEqual(CardRarity.Secret, card.Rarity, card.CardID);
                    Assert.AreNotEqual(StarterCardLibrary.DevilTriggerId, card.CardID);
                }
            }
        }
    }
}
