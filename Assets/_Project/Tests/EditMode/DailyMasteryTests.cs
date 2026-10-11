using System;
using NUnit.Framework;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ทดสอบ Daily Login / Daily Quest / Card Mastery / Collection Milestone</summary>
    public class DailyMasteryTests
    {
        private Func<DateTime> originalNow;
        private DateTime today;

        private static PlayerProfile NewProfile()
        {
            return new PlayerProfile(new SaveData());
        }

        [SetUp]
        public void SetUp()
        {
            originalNow = DailyService.Now;

            today = new DateTime(2026, 10, 1, 12, 0, 0);

            DailyService.Now = () => today;
        }

        [TearDown]
        public void TearDown()
        {
            DailyService.Now = originalNow;
        }

        private static CardData FirstOfRarity(CardRarity rarity)
        {
            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) && card.Rarity == rarity)
                    return card;
            }

            return null;
        }

        // ---------- Daily Login ----------

        [Test]
        public void Login_ClaimsOncePerDay()
        {
            PlayerProfile profile = NewProfile();

            Assert.IsTrue(DailyService.CanClaimLogin(profile));

            LoginReward reward = DailyService.ClaimLogin(profile);

            Assert.IsNotNull(reward);
            Assert.AreEqual(50, profile.Gold);
            Assert.IsFalse(DailyService.CanClaimLogin(profile));
            Assert.IsNull(DailyService.ClaimLogin(profile));
        }

        [Test]
        public void Login_ConsecutiveDaysAdvanceTheCycle()
        {
            PlayerProfile profile = NewProfile();

            DailyService.ClaimLogin(profile);

            today = today.AddDays(1);

            Assert.AreEqual(2, DailyService.NextLoginDay(profile));

            DailyService.ClaimLogin(profile);

            Assert.AreEqual(30, profile.Dust);
        }

        [Test]
        public void Login_MissedDayRestartsTheCycle()
        {
            PlayerProfile profile = NewProfile();

            DailyService.ClaimLogin(profile);

            today = today.AddDays(3);

            Assert.AreEqual(1, DailyService.NextLoginDay(profile));
            Assert.AreEqual(0, DailyService.ClaimedDaysInCycle(profile));

            DailyService.ClaimLogin(profile);

            Assert.AreEqual(100, profile.Gold);
        }

        [Test]
        public void Login_SeventhDayGivesBigRewardThenRestarts()
        {
            PlayerProfile profile = NewProfile();

            for (int i = 0; i < 7; i++)
            {
                DailyService.ClaimLogin(profile);

                if (i < 6)
                    today = today.AddDays(1);
            }

            Assert.AreEqual(7, DailyService.ClaimedDaysInCycle(profile));
            Assert.AreEqual(50 + 80 + 120 + 200, profile.Gold);
            Assert.AreEqual(30 + 50 + 80 + 100, profile.Dust);

            today = today.AddDays(1);

            Assert.AreEqual(1, DailyService.NextLoginDay(profile));
        }

        // ---------- Daily Quest ----------

        [Test]
        public void Quests_AreThreeDistinctAndStableWithinADay()
        {
            PlayerProfile profile = NewProfile();

            DailyService.EnsureToday(profile);

            Assert.AreEqual(DailyService.QuestsPerDay, profile.Data.Quests.Count);

            string first = profile.Data.Quests[0].Id + profile.Data.Quests[1].Id +
                           profile.Data.Quests[2].Id;

            Assert.AreNotEqual(profile.Data.Quests[0].Id, profile.Data.Quests[1].Id);
            Assert.AreNotEqual(profile.Data.Quests[1].Id, profile.Data.Quests[2].Id);

            DailyService.EnsureToday(profile);

            string again = profile.Data.Quests[0].Id + profile.Data.Quests[1].Id +
                           profile.Data.Quests[2].Id;

            Assert.AreEqual(first, again);
        }

        [Test]
        public void Quests_RerollOnANewDay()
        {
            PlayerProfile profile = NewProfile();

            DailyService.EnsureToday(profile);

            profile.Data.Quests[0].Progress = 1;

            today = today.AddDays(1);

            DailyService.EnsureToday(profile);

            Assert.AreEqual(DailyService.TodayKey, profile.Data.QuestDate);
            Assert.AreEqual(0, profile.Data.Quests[0].Progress);
        }

        [Test]
        public void Quests_ProgressClaimAndBonus()
        {
            PlayerProfile profile = NewProfile();

            DailyService.EnsureToday(profile);

            // ทำทุกภารกิจให้สำเร็จโดยรายงานสิ่งที่นับทุกชนิดเกินเป้า
            foreach (QuestMetric metric in Enum.GetValues(typeof(QuestMetric)))
                DailyService.Report(profile, metric, 100);

            Assert.IsTrue(DailyService.HasClaimable(profile));
            Assert.IsFalse(DailyService.CanClaimBonus(profile));

            foreach (DailyQuestState state in profile.Data.Quests)
            {
                Assert.IsTrue(DailyService.IsComplete(state));
                Assert.IsTrue(DailyService.Claim(profile, state.Id));
                Assert.IsFalse(DailyService.Claim(profile, state.Id));
            }

            Assert.IsTrue(DailyService.CanClaimBonus(profile));
            Assert.IsTrue(DailyService.ClaimBonus(profile));
            Assert.IsFalse(DailyService.ClaimBonus(profile));
        }

        // ---------- Card Mastery ----------

        [Test]
        public void Mastery_OnlyForEpicAndAbove()
        {
            Assert.IsFalse(MasteryService.IsEligible(CardRegistry.Get("SUP_001")));

            CardData epic = FirstOfRarity(CardRarity.Epic);

            Assert.IsNotNull(epic);
            Assert.IsTrue(MasteryService.IsEligible(epic));

            PlayerProfile profile = NewProfile();

            MasteryService.AddXp(profile, "SUP_001", 50);

            Assert.AreEqual(0, MasteryService.GetXp(profile, "SUP_001"));
        }

        [Test]
        public void Mastery_LevelsGiveRewardsAndFrames()
        {
            CardData epic = FirstOfRarity(CardRarity.Epic);

            PlayerProfile profile = NewProfile();

            // 20 XP = เลเวล 1 (+20 Gold)
            MasteryService.ApplyBattle(profile, new[] { epic.CardID }, true);

            Assert.AreEqual(1, MasteryService.GetLevel(profile, epic.CardID));
            Assert.AreEqual(20, profile.Gold);
            Assert.IsTrue(MasteryService.FrameColor(1).HasValue);
            Assert.IsFalse(MasteryService.FrameColor(0).HasValue);

            // ใส่ XP จนเต็ม: ได้รางวัลครบทุกเลเวลโดยไม่ซ้ำ
            MasteryService.AddXp(profile, epic.CardID, 1000);

            Assert.AreEqual(MasteryService.MaxLevel, MasteryService.GetLevel(profile, epic.CardID));
            Assert.AreEqual(20 + 0 + 50 + 0 + 100, profile.Gold);
            Assert.AreEqual(30 + 20 + 60 + 100, profile.Dust);

            MasteryService.AddXp(profile, epic.CardID, 1000);

            Assert.AreEqual(170, profile.Gold);
        }

        [Test]
        public void Mastery_LossGivesLessXpThanWin()
        {
            CardData epic = FirstOfRarity(CardRarity.Epic);

            PlayerProfile profile = NewProfile();

            MasteryService.ApplyBattle(profile, new[] { epic.CardID }, false);

            Assert.AreEqual(MasteryService.XpPlayed, MasteryService.GetXp(profile, epic.CardID));
        }

        // ---------- Collection Milestone ----------

        [Test]
        public void Milestone_ClaimOnceAfterReaching()
        {
            PlayerProfile profile = NewProfile();

            MilestoneDef def = MilestoneService.All[0];

            Assert.IsFalse(MilestoneService.CanClaim(profile, def));

            int added = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (!CardRegistry.IsCollectible(card))
                    continue;

                profile.AddCard(card.CardID, 1);

                if (++added >= def.Target)
                    break;
            }

            Assert.IsTrue(MilestoneService.CanClaim(profile, def));
            Assert.IsTrue(MilestoneService.HasClaimable(profile));
            Assert.IsTrue(MilestoneService.Claim(profile, def.Id));
            Assert.IsFalse(MilestoneService.Claim(profile, def.Id));
            Assert.AreEqual(def.RewardGold, profile.Gold);
        }
    }
}
