using System;
using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    public class MilestoneDef
    {
        public string Id;
        public string Title;
        public string Description;
        public Func<PlayerProfile, int> Progress;
        public int Target;
        public int RewardGold;
        public int RewardDust;

        public string RewardText
        {
            get
            {
                string text = "";

                if (RewardGold > 0)
                    text += "+" + RewardGold + " Gold";

                if (RewardDust > 0)
                    text += (text.Length > 0 ? "  " : "") + "+" + RewardDust + " Dust";

                return text;
            }
        }
    }

    /// <summary>
    /// Collection Milestone: เป้าหมายการสะสมการ์ด ถึงเป้าแล้วกดรับรางวัลเอง (รับได้ครั้งเดียว)
    /// </summary>
    public static class MilestoneService
    {
        private static List<MilestoneDef> all;

        public static IReadOnlyList<MilestoneDef> All
        {
            get
            {
                Build();
                return all;
            }
        }

        public static bool IsClaimed(PlayerProfile profile, MilestoneDef def)
        {
            return profile.Data.ClaimedMilestones.Contains(def.Id);
        }

        public static bool IsReached(PlayerProfile profile, MilestoneDef def)
        {
            return def.Progress(profile) >= def.Target;
        }

        public static bool CanClaim(PlayerProfile profile, MilestoneDef def)
        {
            return profile != null && def != null &&
                   !IsClaimed(profile, def) && IsReached(profile, def);
        }

        public static bool Claim(PlayerProfile profile, string id)
        {
            foreach (MilestoneDef def in All)
            {
                if (def.Id != id || !CanClaim(profile, def))
                    continue;

                profile.Data.ClaimedMilestones.Add(def.Id);

                profile.AddGold(def.RewardGold);
                profile.AddDust(def.RewardDust);

                return true;
            }

            return false;
        }

        public static bool HasClaimable(PlayerProfile profile)
        {
            foreach (MilestoneDef def in All)
            {
                if (CanClaim(profile, def))
                    return true;
            }

            return false;
        }

        // ---------- ตัวนับ ----------

        public static int UniqueOwned(PlayerProfile profile)
        {
            int count = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) &&
                    profile.GetOwnedCount(card.CardID) > 0)
                    count++;
            }

            return count;
        }

        public static int OwnedOfRarity(PlayerProfile profile, CardRarity rarity)
        {
            int count = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) &&
                    card.Rarity == rarity &&
                    profile.GetOwnedCount(card.CardID) > 0)
                    count++;
            }

            return count;
        }

        public static int MasteredCards(PlayerProfile profile, int level)
        {
            int count = 0;

            foreach (MasteryEntry entry in profile.Data.Mastery)
            {
                if (MasteryService.LevelFor(entry.Xp) >= level)
                    count++;
            }

            return count;
        }

        private static void Add(
            string id, string title, string description,
            Func<PlayerProfile, int> progress, int target, int gold, int dust)
        {
            all.Add(new MilestoneDef
            {
                Id = id,
                Title = title,
                Description = description,
                Progress = progress,
                Target = target,
                RewardGold = gold,
                RewardDust = dust
            });
        }

        private static void Build()
        {
            if (all != null)
                return;

            all = new List<MilestoneDef>();

            Add("own_10", "Apprentice Collector", "Own 10 different cards",
                UniqueOwned, 10, 50, 0);
            Add("own_25", "Card Gatherer", "Own 25 different cards",
                UniqueOwned, 25, 100, 30);
            Add("own_50", "Seasoned Collector", "Own 50 different cards",
                UniqueOwned, 50, 150, 100);
            Add("own_75", "Grand Collector", "Own 75 different cards",
                UniqueOwned, 75, 200, 150);
            Add("own_100", "Master Collector", "Own 100 different cards",
                UniqueOwned, 100, 300, 200);
            Add("own_150", "Multiverse Archivist", "Own 150 different cards",
                UniqueOwned, 150, 500, 300);

            Add("epic_5", "Epic Taste", "Own 5 different Epic cards",
                p => OwnedOfRarity(p, CardRarity.Epic), 5, 0, 100);
            Add("epic_15", "Epic Hoard", "Own 15 different Epic cards",
                p => OwnedOfRarity(p, CardRarity.Epic), 15, 150, 200);
            Add("legend_3", "Legend Seeker", "Own 3 different Legendary cards",
                p => OwnedOfRarity(p, CardRarity.Legendary), 3, 100, 150);
            Add("legend_8", "Legend Keeper", "Own 8 different Legendary cards",
                p => OwnedOfRarity(p, CardRarity.Legendary), 8, 250, 300);

            Add("mastery_1", "Practiced Hand", "Reach Mastery level 3 with any card",
                p => MasteredCards(p, 3), 1, 100, 50);
            Add("mastery_5", "Master of Many", "Reach Mastery level 3 with 5 cards",
                p => MasteredCards(p, 3), 5, 200, 150);
            Add("mastery_max", "Perfected", "Reach Mastery level 5 with any card",
                p => MasteredCards(p, 5), 1, 200, 200);
        }
    }
}
