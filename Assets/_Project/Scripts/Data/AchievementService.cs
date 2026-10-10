using System;
using System.Collections.Generic;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>สรุปผลการต่อสู้ที่ใช้นับ Achievement</summary>
    public class BattleSummary
    {
        public int Turns;
        public int PlayerHealth;
        public int PlayerMaxHealth;
        public bool OpponentDeckOut;
        public bool SecretSummoned;
        public List<string> SecretIds = new List<string>();
    }

    public class AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        public bool Hidden;

        public Func<PlayerProfile, int> Progress;
        public Func<int> Target;

        public int RewardGold;
        public int RewardDust;

        public int GetTarget()
        {
            return Math.Max(1, Target());
        }
    }

    /// <summary>
    /// Achievement: ตรวจสถิติในโปรไฟล์ ปลดล็อกแล้วแจกรางวัล (Gold / Dust) ทันที
    /// </summary>
    public static class AchievementService
    {
        private static List<AchievementDef> all;

        public static IReadOnlyList<AchievementDef> All
        {
            get
            {
                Build();
                return all;
            }
        }

        public static bool IsUnlocked(PlayerProfile profile, AchievementDef def)
        {
            return profile != null && def != null &&
                profile.Data.Achievements.Contains(def.Id);
        }

        public static int UnlockedCount(PlayerProfile profile)
        {
            int count = 0;

            foreach (AchievementDef def in All)
            {
                if (IsUnlocked(profile, def))
                    count++;
            }

            return count;
        }

        public static string RewardText(AchievementDef def)
        {
            string text = "";

            if (def.RewardGold > 0)
                text += "+" + def.RewardGold + " Gold";

            if (def.RewardDust > 0)
                text += (text.Length > 0 ? "  " : "") + "+" + def.RewardDust + " Dust";

            return text;
        }

        /// <summary>
        /// ตรวจทุก Achievement แล้วปลดล็อกอันที่ถึงเป้า (แจกรางวัล + บันทึก)
        /// คืนรายการที่เพิ่งปลดล็อก
        /// </summary>
        public static List<AchievementDef> Check(PlayerProfile profile)
        {
            List<AchievementDef> unlocked = new List<AchievementDef>();

            if (profile == null)
                return unlocked;

            foreach (AchievementDef def in All)
            {
                if (IsUnlocked(profile, def))
                    continue;

                if (def.Progress(profile) < def.GetTarget())
                    continue;

                profile.Data.Achievements.Add(def.Id);

                profile.AddGold(def.RewardGold);
                profile.AddDust(def.RewardDust);

                unlocked.Add(def);
            }

            if (unlocked.Count > 0)
                MultiverseCardBattle.Core.SaveManager.Save();

            return unlocked;
        }

        public static List<string> ToLines(List<AchievementDef> unlocked)
        {
            List<string> lines = new List<string>();

            foreach (AchievementDef def in unlocked)
            {
                lines.Add(
                    "Achievement unlocked: " + def.Name + "  (" + RewardText(def) + ")");
            }

            return lines;
        }

        // ---------- รายการ ----------

        private static int UniqueOwned(PlayerProfile profile)
        {
            int count = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) &&
                    profile.GetOwnedCount(card.CardID) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CollectibleTotal()
        {
            int count = 0;

            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card))
                    count++;
            }

            return count;
        }

        private static int OwnedLegendary(PlayerProfile profile)
        {
            foreach (CardData card in CardRegistry.All)
            {
                if (CardRegistry.IsCollectible(card) &&
                    card.Rarity == CardRarity.Legendary &&
                    profile.GetOwnedCount(card.CardID) > 0)
                {
                    return 1;
                }
            }

            return 0;
        }

        private static void Add(
            string id, string name, string description,
            Func<PlayerProfile, int> progress, Func<int> target,
            int gold, int dust, bool hidden = false)
        {
            AchievementDef def = new AchievementDef();

            def.Id = id;
            def.Name = name;
            def.Description = description;
            def.Progress = progress;
            def.Target = target;
            def.RewardGold = gold;
            def.RewardDust = dust;
            def.Hidden = hidden;

            all.Add(def);
        }

        private static void Build()
        {
            if (all != null)
                return;

            all = new List<AchievementDef>();

            Add("first_win", "First Blood", "Win a battle",
                p => p.Data.StatWins, () => 1, 50, 0);

            Add("win_10", "Veteran", "Win 10 battles",
                p => p.Data.StatWins, () => 10, 200, 0);

            Add("win_50", "Champion", "Win 50 battles",
                p => p.Data.StatWins, () => 50, 500, 100);

            Add("battles_25", "Seasoned Duelist", "Play 25 battles",
                p => p.Data.StatBattles, () => 25, 150, 0);

            Add("clear_3", "Adventurer", "Clear 3 campaign stages",
                p => p.Data.CompletedStages.Count, () => 3, 150, 0);

            Add("clear_all", "Conqueror", "Clear every campaign stage",
                p => p.Data.CompletedStages.Count,
                () => CampaignLibrary.Stages.Count, 1000, 200);

            Add("pack_1", "Unboxing", "Open your first pack",
                p => p.Data.StatPacks, () => 1, 0, 40);

            Add("pack_10", "Pack Rat", "Open 10 packs",
                p => p.Data.StatPacks, () => 10, 200, 0);

            Add("pack_30", "Pack Master", "Open 30 packs",
                p => p.Data.StatPacks, () => 30, 0, 300);

            Add("collect_25", "Collector", "Own 25 different cards",
                UniqueOwned, () => 25, 0, 100);

            Add("collect_50", "Archivist", "Own 50 different cards",
                UniqueOwned, () => 50, 300, 250);

            Add("collect_all", "Completionist", "Own every collectible card",
                UniqueOwned, CollectibleTotal, 1000, 500);

            Add("legend", "Legend Hunter", "Own a Legendary card",
                OwnedLegendary, () => 1, 0, 100);

            Add("craft_1", "Artisan", "Craft a card with Dust",
                p => p.Data.StatCrafted, () => 1, 60, 0);

            Add("disenchant_10", "Recycler", "Disenchant 10 cards",
                p => p.Data.StatDisenchanted, () => 10, 100, 0);

            Add("flawless", "Untouchable", "Win a battle without losing any HP",
                p => p.Data.StatFlawlessWins, () => 1, 100, 0);

            Add("comeback", "Close Call", "Win a battle with 3 HP or less left",
                p => p.Data.StatComebackWins, () => 1, 100, 0);

            Add("blitz", "Blitz", "Win a battle within 6 turns",
                p => p.Data.StatFastWins, () => 1, 100, 0);

            Add("deckout", "Library Closed", "Win because your opponent ran out of cards",
                p => p.Data.StatDeckOutWins, () => 1, 150, 0);

            Add("secret", "Shadow Monarch",
                "Something stirs in the shadows when you summon...",
                p => p.Data.StatSecrets, () => 1, 500, 300, true);

            Add("knight", "A Knight's Duty",
                "Prove yourself in battle... a knight watches from the abyss.",
                p => p.GetOwnedCount(StarterCardLibrary.ArtoriasId) > 0 ? 1 : 0,
                () => 1, 500, 300, true);

            Add("nazarick", "The Throne of Nazarick",
                "Cast many spells and let the dead pile up... something watches.",
                p => p.GetOwnedCount(StarterCardLibrary.AinzId) > 0 ? 1 : 0,
                () => 1, 500, 300, true);
        }
    }
}
