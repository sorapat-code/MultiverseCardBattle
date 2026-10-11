using System;
using System.Collections.Generic;
using System.Globalization;

namespace MultiverseCardBattle.Data
{
    /// <summary>สิ่งที่ภารกิจรายวันนับความคืบหน้า</summary>
    public enum QuestMetric
    {
        WinBattles,
        PlayBattles,
        PlayCards,
        PlaySpells,
        KillUnits,
        OpenPacks,
        LeaderDamage,
        CraftOrDisenchant
    }

    public class QuestDef
    {
        public string Id;
        public string Description;
        public QuestMetric Metric;
        public int Target;
        public int RewardGold;
        public int RewardDust;
    }

    public class LoginReward
    {
        public int Gold;
        public int Dust;

        public string Text
        {
            get
            {
                string text = "";

                if (Gold > 0)
                    text += "+" + Gold + " Gold";

                if (Dust > 0)
                    text += (text.Length > 0 ? "  " : "") + "+" + Dust + " Dust";

                return text;
            }
        }
    }

    /// <summary>
    /// Daily Login (วงจร 7 วัน ขาดวันเริ่มใหม่) และ Daily Quest (สุ่ม 3 ภารกิจต่อวัน)
    /// เวลาใช้วันที่ตามเครื่องผู้เล่น เปลี่ยน Now ได้เพื่อทดสอบ
    /// ผู้เรียกต้องเรียก SaveManager.Save() เองหลังเปลี่ยนข้อมูล
    /// </summary>
    public static class DailyService
    {
        public const int QuestsPerDay = 3;
        public const int BonusGold = 100;
        public const int BonusDust = 50;

        public static Func<DateTime> Now = () => DateTime.Now;

        public static readonly LoginReward[] LoginRewards =
        {
            new LoginReward { Gold = 50 },
            new LoginReward { Dust = 30 },
            new LoginReward { Gold = 80 },
            new LoginReward { Dust = 50 },
            new LoginReward { Gold = 120 },
            new LoginReward { Dust = 80 },
            new LoginReward { Gold = 200, Dust = 100 }
        };

        private static List<QuestDef> pool;

        public static IReadOnlyList<QuestDef> Pool
        {
            get
            {
                BuildPool();
                return pool;
            }
        }

        public static string Key(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static string TodayKey
        {
            get { return Key(Now()); }
        }

        /// <summary>เวลาที่เหลือก่อนรีเซ็ต (เที่ยงคืนตามเครื่อง)</summary>
        public static TimeSpan TimeUntilReset
        {
            get
            {
                DateTime now = Now();

                return now.Date.AddDays(1) - now;
            }
        }

        // ---------- Daily Login ----------

        public static bool CanClaimLogin(PlayerProfile profile)
        {
            return profile != null && profile.Data.LoginLastClaim != TodayKey;
        }

        /// <summary>วันที่ในวงจร (1-7) ของรางวัลที่จะได้เมื่อรับ</summary>
        public static int NextLoginDay(PlayerProfile profile)
        {
            if (profile == null)
                return 1;

            int streak = profile.Data.LoginStreak;

            // ขาดไปอย่างน้อย 1 วัน วงจรเริ่มใหม่
            if (profile.Data.LoginLastClaim != Key(Now().Date.AddDays(-1)) &&
                profile.Data.LoginLastClaim != TodayKey)
                streak = 0;

            return streak % LoginRewards.Length + 1;
        }

        /// <summary>วันที่ที่รับไปแล้วในวงจรปัจจุบัน (0-7) ใช้แสดงผล</summary>
        public static int ClaimedDaysInCycle(PlayerProfile profile)
        {
            if (profile == null)
                return 0;

            string last = profile.Data.LoginLastClaim;

            bool alive = last == TodayKey || last == Key(Now().Date.AddDays(-1));

            if (!alive)
                return 0;

            int streak = profile.Data.LoginStreak;

            if (streak <= 0)
                return 0;

            int inCycle = streak % LoginRewards.Length;

            // รับวันที่ 7 แล้ว (ครบวงจร) และยังเป็นวันนี้ ให้แสดงว่าครบ 7
            if (inCycle == 0)
                return last == TodayKey ? LoginRewards.Length : 0;

            return inCycle;
        }

        public static LoginReward ClaimLogin(PlayerProfile profile)
        {
            if (!CanClaimLogin(profile))
                return null;

            SaveData data = profile.Data;

            if (data.LoginLastClaim != Key(Now().Date.AddDays(-1)))
                data.LoginStreak = 0;

            LoginReward reward = LoginRewards[data.LoginStreak % LoginRewards.Length];

            data.LoginStreak++;
            data.LoginLastClaim = TodayKey;

            profile.AddGold(reward.Gold);
            profile.AddDust(reward.Dust);

            return reward;
        }

        // ---------- Daily Quest ----------

        private static void BuildPool()
        {
            if (pool != null)
                return;

            pool = new List<QuestDef>();

            Add("win_1", "Win 1 battle", QuestMetric.WinBattles, 1, 60, 0);
            Add("win_2", "Win 2 battles", QuestMetric.WinBattles, 2, 110, 0);
            Add("play_3", "Play 3 battles", QuestMetric.PlayBattles, 3, 70, 0);
            Add("cards_15", "Play 15 cards", QuestMetric.PlayCards, 15, 60, 0);
            Add("spells_5", "Cast 5 spells", QuestMetric.PlaySpells, 5, 0, 40);
            Add("kill_8", "Destroy 8 enemy units", QuestMetric.KillUnits, 8, 60, 0);
            Add("pack_1", "Open 1 pack", QuestMetric.OpenPacks, 1, 0, 30);
            Add("damage_20", "Deal 20 damage to enemy Leaders", QuestMetric.LeaderDamage, 20, 70, 0);
            Add("craft_2", "Craft or disenchant 2 cards", QuestMetric.CraftOrDisenchant, 2, 0, 30);
        }

        private static void Add(
            string id, string description, QuestMetric metric,
            int target, int gold, int dust)
        {
            pool.Add(new QuestDef
            {
                Id = id,
                Description = description,
                Metric = metric,
                Target = target,
                RewardGold = gold,
                RewardDust = dust
            });
        }

        public static QuestDef GetDef(string id)
        {
            foreach (QuestDef def in Pool)
            {
                if (def.Id == id)
                    return def;
            }

            return null;
        }

        /// <summary>สุ่มชุดภารกิจของวันนี้ถ้ายังไม่มี (วันเดียวกัน ชุดเดิม)</summary>
        public static void EnsureToday(PlayerProfile profile)
        {
            if (profile == null)
                return;

            SaveData data = profile.Data;

            if (data.QuestDate == TodayKey && data.Quests.Count > 0)
                return;

            data.QuestDate = TodayKey;
            data.QuestBonusClaimed = false;
            data.Quests = new List<DailyQuestState>();

            // ใช้วันที่เป็น seed: ทุกครั้งที่เปิดในวันเดียวกันได้ชุดเดิม
            // (คำนวณเอง ไม่ใช้ GetHashCode เพราะค่าอาจไม่คงที่ระหว่างการรัน)
            int seed = 17;

            foreach (char c in TodayKey)
                seed = unchecked(seed * 31 + c);

            Random rng = new Random(seed & 0x7fffffff);

            List<QuestDef> candidates = new List<QuestDef>(Pool);

            for (int i = 0; i < QuestsPerDay && candidates.Count > 0; i++)
            {
                int index = rng.Next(candidates.Count);

                DailyQuestState state = new DailyQuestState();

                state.Id = candidates[index].Id;

                data.Quests.Add(state);

                candidates.RemoveAt(index);
            }
        }

        public static List<DailyQuestState> Quests(PlayerProfile profile)
        {
            EnsureToday(profile);

            return profile.Data.Quests;
        }

        /// <summary>เพิ่มความคืบหน้าให้ภารกิจของวันนี้ที่นับสิ่งนี้</summary>
        public static void Report(PlayerProfile profile, QuestMetric metric, int amount)
        {
            if (profile == null || amount <= 0)
                return;

            EnsureToday(profile);

            foreach (DailyQuestState state in profile.Data.Quests)
            {
                QuestDef def = GetDef(state.Id);

                if (def == null || def.Metric != metric || state.Claimed)
                    continue;

                state.Progress = Math.Min(def.Target, state.Progress + amount);
            }
        }

        public static bool IsComplete(DailyQuestState state)
        {
            QuestDef def = state != null ? GetDef(state.Id) : null;

            return def != null && state.Progress >= def.Target;
        }

        public static bool Claim(PlayerProfile profile, string questId)
        {
            if (profile == null)
                return false;

            EnsureToday(profile);

            foreach (DailyQuestState state in profile.Data.Quests)
            {
                if (state.Id != questId || state.Claimed || !IsComplete(state))
                    continue;

                QuestDef def = GetDef(state.Id);

                state.Claimed = true;

                profile.AddGold(def.RewardGold);
                profile.AddDust(def.RewardDust);

                return true;
            }

            return false;
        }

        public static bool AllClaimed(PlayerProfile profile)
        {
            if (profile == null)
                return false;

            EnsureToday(profile);

            if (profile.Data.Quests.Count == 0)
                return false;

            foreach (DailyQuestState state in profile.Data.Quests)
            {
                if (!state.Claimed)
                    return false;
            }

            return true;
        }

        public static bool CanClaimBonus(PlayerProfile profile)
        {
            return AllClaimed(profile) && !profile.Data.QuestBonusClaimed;
        }

        public static bool ClaimBonus(PlayerProfile profile)
        {
            if (!CanClaimBonus(profile))
                return false;

            profile.Data.QuestBonusClaimed = true;

            profile.AddGold(BonusGold);
            profile.AddDust(BonusDust);

            return true;
        }

        /// <summary>มีอะไรให้รับอยู่ (ใช้โชว์จุดแจ้งเตือนที่ปุ่มเมนู)</summary>
        public static bool HasClaimable(PlayerProfile profile)
        {
            if (profile == null)
                return false;

            if (CanClaimLogin(profile))
                return true;

            EnsureToday(profile);

            foreach (DailyQuestState state in profile.Data.Quests)
            {
                if (!state.Claimed && IsComplete(state))
                    return true;
            }

            return CanClaimBonus(profile);
        }

        /// <summary>รายงานผลการต่อสู้ให้ภารกิจรายวัน</summary>
        public static void ReportBattle(
            PlayerProfile profile, bool won, BattleSummary summary)
        {
            Report(profile, QuestMetric.PlayBattles, 1);

            if (won)
                Report(profile, QuestMetric.WinBattles, 1);

            if (summary == null)
                return;

            Report(profile, QuestMetric.PlayCards, summary.CardsPlayed);
            Report(profile, QuestMetric.PlaySpells, summary.SpellsPlayed);
            Report(profile, QuestMetric.KillUnits, summary.UnitsKilled);
            Report(profile, QuestMetric.LeaderDamage, summary.LeaderDamage);
        }
    }
}
