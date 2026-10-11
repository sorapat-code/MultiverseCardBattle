using NUnit.Framework;
using MultiverseCardBattle.Data;

namespace MultiverseCardBattle.Tests
{
    /// <summary>ทดสอบประวัติแมตช์ / สถิติรวม และตัวคูณระดับความยาก AI</summary>
    public class StatsDifficultyTests
    {
        private static PlayerProfile NewProfile()
        {
            return new PlayerProfile(new SaveData());
        }

        private static MatchRecord Match(int result, int difficulty = 1)
        {
            MatchRecord record = new MatchRecord();

            record.Date = "2026-10-11 12:00";
            record.Result = result;
            record.Opponent = "Dummy";
            record.Mode = "Quick Battle";
            record.Difficulty = difficulty;
            record.Turns = 6;
            record.CardsPlayed = 8;
            record.UnitsKilled = 3;
            record.LeaderDamage = 12;

            return record;
        }

        [Test]
        public void WinStreakCountsAndLossResetsIt()
        {
            PlayerProfile profile = NewProfile();

            StatsService.Record(profile, Match(1), null);
            StatsService.Record(profile, Match(1), null);
            StatsService.Record(profile, Match(1), null);

            Assert.AreEqual(3, profile.Data.StatWinStreak);
            Assert.AreEqual(3, profile.Data.StatBestStreak);

            StatsService.Record(profile, Match(0), null);

            Assert.AreEqual(0, profile.Data.StatWinStreak);
            Assert.AreEqual(3, profile.Data.StatBestStreak);
        }

        [Test]
        public void DrawKeepsStreakAndIsCounted()
        {
            PlayerProfile profile = NewProfile();

            StatsService.Record(profile, Match(1), null);
            StatsService.Record(profile, Match(2), null);

            Assert.AreEqual(1, profile.Data.StatWinStreak);
            Assert.AreEqual(1, profile.Data.StatDraws);
        }

        [Test]
        public void HistoryIsCappedAndKeepsNewest()
        {
            PlayerProfile profile = NewProfile();

            for (int i = 0; i < StatsService.MaxHistory + 10; i++)
            {
                MatchRecord record = Match(1);

                record.Turns = i;

                StatsService.Record(profile, record, null);
            }

            Assert.AreEqual(StatsService.MaxHistory, profile.Data.History.Count);
            Assert.AreEqual(
                StatsService.MaxHistory + 9,
                profile.Data.History[profile.Data.History.Count - 1].Turns);
        }

        [Test]
        public void DifficultyAndCardUsageAreTracked()
        {
            PlayerProfile profile = NewProfile();

            StatsService.Record(
                profile, Match(1, 2), new[] { "SUP_001", "MAG_001" });
            StatsService.Record(
                profile, Match(0, 2), new[] { "SUP_001" });

            Assert.AreEqual(2, profile.Data.DifficultyBattles[2]);
            Assert.AreEqual(1, profile.Data.DifficultyWins[2]);

            var top = StatsService.TopCards(profile, 5);

            Assert.AreEqual("SUP_001", top[0].CardID);
            Assert.AreEqual(2, top[0].Matches);
            Assert.AreEqual(1, top[0].Wins);
        }

        [Test]
        public void OverviewTextDoesNotThrowOnEmptyProfile()
        {
            Assert.IsNotEmpty(StatsService.BuildOverview(NewProfile()));
        }

        [Test]
        public void DifficultyScalesAreOrdered()
        {
            Assert.Less(
                AiDifficultyInfo.EnemyHealthScale(AiDifficulty.Easy),
                AiDifficultyInfo.EnemyHealthScale(AiDifficulty.Normal));

            Assert.Less(
                AiDifficultyInfo.EnemyHealthScale(AiDifficulty.Normal),
                AiDifficultyInfo.EnemyHealthScale(AiDifficulty.Hard));

            Assert.AreEqual(1f, AiDifficultyInfo.GoldScale(AiDifficulty.Normal));
            Assert.AreEqual(AiDifficulty.Hard, AiDifficultyInfo.FromInt(99));
            Assert.AreEqual(AiDifficulty.Easy, AiDifficultyInfo.FromInt(-1));
        }
    }
}
