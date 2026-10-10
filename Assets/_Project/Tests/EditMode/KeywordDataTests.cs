using System;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>
    /// ตรวจว่า Keyword / Status ทุกตัวมีคำอธิบายและชื่อแสดงผล
    /// </summary>
    public class KeywordDataTests
    {
        [Test]
        public void EveryKeyword_HasADescription()
        {
            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                Assert.IsNotEmpty(
                    GameGlossary.KeywordDescription(keyword),
                    keyword + " has no description");
            }
        }

        [Test]
        public void EveryStatus_HasANameAndDescription()
        {
            foreach (StatusType status in Enum.GetValues(typeof(StatusType)))
            {
                if (status == StatusType.None)
                    continue;

                Assert.IsNotEmpty(StatusInfo.DisplayName(status));
                Assert.IsNotEmpty(
                    StatusInfo.Description(status),
                    status + " has no description");
            }
        }

        [Test]
        public void KeywordFlags_AreUniqueBits()
        {
            Keyword seen = Keyword.None;

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                Assert.AreEqual(Keyword.None, seen & keyword,
                    keyword + " overlaps another keyword");

                seen |= keyword;
            }
        }

        [Test]
        public void KeywordName_AddsSpaces()
        {
            Assert.AreEqual("Quick Attack",
                CardTextGenerator.KeywordName(Keyword.QuickAttack));
            Assert.AreEqual("Double Strike",
                CardTextGenerator.KeywordName(Keyword.DoubleStrike));
            Assert.AreEqual("Taunt",
                CardTextGenerator.KeywordName(Keyword.Taunt));
        }

        [Test]
        public void Glossary_MentionsEveryKeywordAndStatus()
        {
            string text = GameGlossary.Build();

            foreach (Keyword keyword in Enum.GetValues(typeof(Keyword)))
            {
                if (keyword == Keyword.None)
                    continue;

                StringAssert.Contains(
                    CardTextGenerator.KeywordName(keyword), text);
            }

            foreach (StatusType status in Enum.GetValues(typeof(StatusType)))
            {
                if (status == StatusType.None)
                    continue;

                StringAssert.Contains(StatusInfo.DisplayName(status), text);
            }
        }

        [Test]
        public void StatusStacking_RulesAreConsistent()
        {
            foreach (StatusType status in Enum.GetValues(typeof(StatusType)))
            {
                Assert.IsFalse(
                    StatusInfo.Stacks(status) && StatusInfo.Refreshes(status),
                    status + " cannot both stack and refresh");
            }
        }
    }
}
