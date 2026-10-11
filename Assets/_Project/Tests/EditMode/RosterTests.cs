using System.Collections.Generic;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ตรวจชุดการ์ดปัจจุบัน: ตัวประกอบ (SUP_) ไม่เกิน Epic, เวทมนตร์ (MAG_), การ์ดลับไม่หลุดเข้าแพ็ก</summary>
    public class RosterTests
    {
        [Test]
        public void Roster_HasExpectedSupportUnitsAndSpells()
        {
            int units = 0;
            int spells = 0;

            foreach (CardData card in StarterCardLibrary.All)
            {
                if (card.CardID.StartsWith("SUP_"))
                {
                    units++;
                    Assert.AreEqual(CardType.Unit, card.Type, card.CardID);
                }
                else if (card.CardID.StartsWith("MAG_"))
                {
                    spells++;
                    Assert.AreEqual(CardType.Spell, card.Type, card.CardID);
                }
            }

            Assert.AreEqual(30, units);
            Assert.AreEqual(15, spells);
        }

        [Test]
        public void Equipment_HasFiveUnitGearAndThreeLeaderGear_NeverAboveEpic()
        {
            int unitGear = 0;
            int leaderGear = 0;

            foreach (CardData card in StarterCardLibrary.All)
            {
                if (!card.CardID.StartsWith("EQP_"))
                    continue;

                Assert.AreEqual(CardType.Equipment, card.Type, card.CardID);
                Assert.LessOrEqual((int)card.Rarity, (int)CardRarity.Epic, card.CardID);
                Assert.AreEqual(CardSet.Core, card.Set, card.CardID);

                if (card.GearDurability > 0)
                    leaderGear++;
                else
                    unitGear++;
            }

            Assert.AreEqual(5, unitGear);
            Assert.AreEqual(3, leaderGear);
        }

        [Test]
        public void SupportingCharacters_AreNeverAboveEpic()
        {
            foreach (CardData card in StarterCardLibrary.All)
            {
                if (!card.CardID.StartsWith("SUP_"))
                    continue;

                Assert.LessOrEqual((int)card.Rarity, (int)CardRarity.Epic, card.CardID);
            }
        }

        [Test]
        public void SecretCards_AreKeptAndStayOutOfEveryPool()
        {
            Assert.IsNotNull(StarterCardLibrary.Get(StarterCardLibrary.SecretId));
            Assert.IsNotNull(StarterCardLibrary.Get(StarterCardLibrary.ArtoriasId));
            Assert.IsNotNull(StarterCardLibrary.Get(StarterCardLibrary.AinzId));
            Assert.IsNotNull(StarterCardLibrary.Get(StarterCardLibrary.VergilId));
            Assert.IsNotNull(StarterCardLibrary.Get(StarterCardLibrary.DevilTriggerId));

            foreach (CardSet set in new[] { CardSet.Core, CardSet.Rift })
            {
                foreach (CardData card in PackService.PoolOf(set))
                {
                    Assert.AreNotEqual(CardRarity.Secret, card.Rarity, card.CardID);
                    Assert.AreNotEqual(StarterCardLibrary.DevilTriggerId, card.CardID);
                    Assert.AreNotEqual(CardSource.Token, card.Source, card.CardID);
                }
            }
        }

        [Test]
        public void CorePool_ContainsEveryCollectibleCard()
        {
            List<CardData> pool = PackService.PoolOf(CardSet.Core);

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card))
                    Assert.IsTrue(pool.Contains(card), card.CardID);
            }
        }

        [Test]
        public void LeaderTokensAndFinishers_StillExist()
        {
            foreach (string id in new[]
            {
                StarterCardLibrary.CoinId, StarterCardLibrary.ShadowCloneId,
                StarterCardLibrary.SpiritBombId, StarterCardLibrary.TailedBeastBombId,
                StarterCardLibrary.HollowPurpleId, StarterCardLibrary.ProtonCannonId,
                StarterCardLibrary.HeroEchoId, StarterCardLibrary.AmbushId,
                StarterCardLibrary.DivineJudgmentId, StarterCardLibrary.GateCollapseId,
                StarterCardLibrary.EmperorWrathId, StarterCardLibrary.WorldRewriteId
            })
            {
                Assert.IsNotNull(StarterCardLibrary.Get(id), id);
            }
        }
    }
}
