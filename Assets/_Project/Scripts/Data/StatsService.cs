using System;
using System.Collections.Generic;
using System.Text;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// สถิติรวมและประวัติแมตช์ของผู้เล่น (เก็บใน SaveData)
    /// </summary>
    public static class StatsService
    {
        public const int MaxHistory = 50;

        public static string ResultName(int result)
        {
            switch (result)
            {
                case 1: return "WIN";
                case 2: return "DRAW";
                default: return "LOSS";
            }
        }

        private static void EnsureDifficultyLists(SaveData data)
        {
            if (data.DifficultyBattles == null)
                data.DifficultyBattles = new List<int>();

            if (data.DifficultyWins == null)
                data.DifficultyWins = new List<int>();

            while (data.DifficultyBattles.Count < 3)
                data.DifficultyBattles.Add(0);

            while (data.DifficultyWins.Count < 3)
                data.DifficultyWins.Add(0);
        }

        /// <summary>
        /// บันทึกแมตช์ที่จบแล้ว: เพิ่มเข้าประวัติ อัปเดตสถิติรวม สตรีค และการ์ดที่ใช้
        /// </summary>
        public static void Record(
            PlayerProfile profile,
            MatchRecord record,
            IEnumerable<string> playedCardIds)
        {
            if (profile == null || record == null)
                return;

            SaveData data = profile.Data;

            if (data.History == null)
                data.History = new List<MatchRecord>();

            data.History.Add(record);

            while (data.History.Count > MaxHistory)
                data.History.RemoveAt(0);

            bool won = record.Result == 1;

            if (record.Result == 2)
            {
                data.StatDraws++;
            }
            else if (won)
            {
                data.StatWinStreak++;

                if (data.StatWinStreak > data.StatBestStreak)
                    data.StatBestStreak = data.StatWinStreak;
            }
            else
            {
                data.StatWinStreak = 0;
            }

            data.StatTotalTurns += record.Turns;
            data.StatTotalKills += record.UnitsKilled;
            data.StatTotalCardsPlayed += record.CardsPlayed;
            data.StatTotalLeaderDamage += record.LeaderDamage;

            EnsureDifficultyLists(data);

            int level = Math.Max(0, Math.Min(2, record.Difficulty));

            data.DifficultyBattles[level]++;

            if (won)
                data.DifficultyWins[level]++;

            if (playedCardIds == null)
                return;

            if (data.CardUsage == null)
                data.CardUsage = new List<CardUsageEntry>();

            foreach (string id in playedCardIds)
            {
                if (string.IsNullOrEmpty(id))
                    continue;

                CardUsageEntry entry = data.CardUsage.Find(e => e.CardID == id);

                if (entry == null)
                {
                    entry = new CardUsageEntry();
                    entry.CardID = id;

                    data.CardUsage.Add(entry);
                }

                entry.Matches++;

                if (won)
                    entry.Wins++;
            }
        }

        /// <summary>การ์ดที่ใช้บ่อยที่สุด (ตามจำนวนแมตช์)</summary>
        public static List<CardUsageEntry> TopCards(PlayerProfile profile, int count)
        {
            List<CardUsageEntry> list = new List<CardUsageEntry>();

            if (profile == null || profile.Data.CardUsage == null)
                return list;

            list.AddRange(profile.Data.CardUsage);

            list.Sort((a, b) =>
            {
                int byMatches = b.Matches.CompareTo(a.Matches);

                return byMatches != 0 ? byMatches : b.Wins.CompareTo(a.Wins);
            });

            if (list.Count > count)
                list.RemoveRange(count, list.Count - count);

            return list;
        }

        public static float WinRate(int wins, int battles)
        {
            return battles <= 0 ? 0f : (float)wins / battles;
        }

        private static string Percent(int wins, int battles)
        {
            return battles <= 0
                ? "-"
                : Math.Round(WinRate(wins, battles) * 100f) + "%";
        }

        /// <summary>ข้อความสรุปสถิติรวม (Rich Text) สำหรับหน้า Stats</summary>
        public static string BuildOverview(PlayerProfile profile)
        {
            SaveData d = profile.Data;

            EnsureDifficultyLists(d);

            int battles = d.StatBattles;
            int losses = Math.Max(0, battles - d.StatWins - d.StatDraws);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<b>Overall</b>");
            sb.AppendLine("Battles: " + battles);
            sb.AppendLine("Wins: " + d.StatWins + "   Losses: " + losses +
                "   Draws: " + d.StatDraws);
            sb.AppendLine("Win rate: " + Percent(d.StatWins, battles));
            sb.AppendLine("Current streak: " + d.StatWinStreak +
                "   Best streak: " + d.StatBestStreak);
            sb.AppendLine();

            sb.AppendLine("<b>By difficulty</b>");

            for (int i = 0; i < 3; i++)
            {
                sb.AppendLine(
                    AiDifficultyInfo.Names[i] + ": " +
                    d.DifficultyWins[i] + " / " + d.DifficultyBattles[i] +
                    "  (" + Percent(d.DifficultyWins[i], d.DifficultyBattles[i]) + ")");
            }

            sb.AppendLine();

            sb.AppendLine("<b>Totals</b>");
            sb.AppendLine("Cards played: " + d.StatTotalCardsPlayed);
            sb.AppendLine("Enemy units destroyed: " + d.StatTotalKills);
            sb.AppendLine("Damage to enemy Leaders: " + d.StatTotalLeaderDamage);

            if (battles > 0)
            {
                sb.AppendLine("Average match length: " +
                    (d.StatTotalTurns / (float)battles).ToString("0.0") + " turns");
            }

            sb.AppendLine("Packs opened: " + d.StatPacks +
                "   Crafted: " + d.StatCrafted);
            sb.AppendLine();

            sb.AppendLine("<b>Most played cards</b>");

            List<CardUsageEntry> top = TopCards(profile, 5);

            if (top.Count == 0)
            {
                sb.AppendLine("Play a few matches first.");
            }

            foreach (CardUsageEntry entry in top)
            {
                CardData card = CardRegistry.Get(entry.CardID);

                string name = card != null ? card.CardName : entry.CardID;

                sb.AppendLine(name + " - " + entry.Matches + " matches (" +
                    Percent(entry.Wins, entry.Matches) + " won)");
            }

            return sb.ToString();
        }

        /// <summary>บรรทัดเดียวของประวัติแมตช์ (Rich Text)</summary>
        public static string FormatRecord(MatchRecord r)
        {
            string color = r.Result == 1 ? "#70E080" : (r.Result == 2 ? "#E0D070" : "#FF8080");

            return "<color=" + color + "><b>" + ResultName(r.Result) + "</b></color>   " +
                r.Opponent + "  -  " + r.Mode + "  (" +
                AiDifficultyInfo.NameOf(AiDifficultyInfo.FromInt(r.Difficulty)) + ")";
        }

        public static string FormatRecordDetail(MatchRecord r)
        {
            return r.Date + "   " + r.Turns + " turns   HP left " + r.HealthLeft +
                "   Cards " + r.CardsPlayed + "   Kills " + r.UnitsKilled +
                "   Gold +" + r.Gold;
        }
    }
}
