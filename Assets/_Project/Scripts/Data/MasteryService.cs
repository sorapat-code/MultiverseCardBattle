using System;
using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// Card Mastery: เฉพาะการ์ด Epic ขึ้นไป ได้ XP จากการใช้การ์ดในการต่อสู้
    /// เลเวลขึ้นได้ Gold/Dust และกรอบการ์ดพิเศษ (Bronze / Gold / Platinum)
    /// </summary>
    public static class MasteryService
    {
        public const int MaxLevel = 5;
        public const int XpPlayed = 10;
        public const int XpWon = 10;

        // XP สะสมที่ต้องใช้ถึงเลเวล 1..5
        private static readonly int[] Thresholds = { 20, 60, 120, 200, 300 };

        private static readonly int[] RewardGold = { 20, 0, 50, 0, 100 };
        private static readonly int[] RewardDust = { 0, 30, 20, 60, 100 };

        public static bool IsEligible(CardData card)
        {
            if (card == null || card.Type == CardType.Leader)
                return false;

            if (card.Source == CardSource.Token || card.Source == CardSource.Debug)
                return false;

            return card.Rarity == CardRarity.Epic ||
                   card.Rarity == CardRarity.Legendary ||
                   card.Rarity == CardRarity.Secret;
        }

        public static int GetXp(PlayerProfile profile, string cardId)
        {
            MasteryEntry entry = Find(profile, cardId);

            return entry != null ? entry.Xp : 0;
        }

        public static int GetLevel(PlayerProfile profile, string cardId)
        {
            return LevelFor(GetXp(profile, cardId));
        }

        public static int LevelFor(int xp)
        {
            int level = 0;

            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (xp >= Thresholds[i])
                    level = i + 1;
            }

            return level;
        }

        /// <summary>XP สะสมที่ต้องใช้ถึงเลเวลนี้ (0 = เริ่มต้น)</summary>
        public static int XpAt(int level)
        {
            if (level <= 0)
                return 0;

            return Thresholds[Math.Min(level, MaxLevel) - 1];
        }

        public static string RewardText(int level)
        {
            if (level < 1 || level > MaxLevel)
                return "";

            string text = "";

            if (RewardGold[level - 1] > 0)
                text += "+" + RewardGold[level - 1] + " Gold";

            if (RewardDust[level - 1] > 0)
                text += (text.Length > 0 ? "  " : "") + "+" + RewardDust[level - 1] + " Dust";

            return text;
        }

        public static string FrameName(int level)
        {
            if (level >= 5)
                return "Platinum Frame";

            if (level >= 3)
                return "Gold Frame";

            if (level >= 1)
                return "Bronze Frame";

            return "";
        }

        /// <summary>สีกรอบการ์ดตามเลเวล (null = ใช้กรอบตามความหายากปกติ)</summary>
        public static Color? FrameColor(int level)
        {
            if (level >= 5)
                return new Color(0.72f, 0.95f, 1f, 1f);

            if (level >= 3)
                return new Color(1f, 0.84f, 0.28f, 1f);

            if (level >= 1)
                return new Color(0.82f, 0.52f, 0.28f, 1f);

            return null;
        }

        private static MasteryEntry Find(PlayerProfile profile, string cardId)
        {
            if (profile == null || string.IsNullOrEmpty(cardId))
                return null;

            foreach (MasteryEntry entry in profile.Data.Mastery)
            {
                if (entry.CardID == cardId)
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// เพิ่ม XP ให้การ์ด (เฉพาะที่เข้าเงื่อนไข) แจกรางวัลเลเวลที่ขึ้นทันที
        /// คืนข้อความสรุปเลเวลที่ขึ้น
        /// </summary>
        public static List<string> AddXp(PlayerProfile profile, string cardId, int amount)
        {
            List<string> lines = new List<string>();

            CardData card = CardRegistry.Get(cardId);

            if (profile == null || amount <= 0 || !IsEligible(card))
                return lines;

            MasteryEntry entry = Find(profile, cardId);

            if (entry == null)
            {
                entry = new MasteryEntry();

                entry.CardID = cardId;

                profile.Data.Mastery.Add(entry);
            }

            entry.Xp = Math.Min(Thresholds[MaxLevel - 1], entry.Xp + amount);

            int level = LevelFor(entry.Xp);

            while (entry.RewardedLevel < level)
            {
                entry.RewardedLevel++;

                profile.AddGold(RewardGold[entry.RewardedLevel - 1]);
                profile.AddDust(RewardDust[entry.RewardedLevel - 1]);

                lines.Add(
                    "Mastery: " + card.CardName + " reached level " + entry.RewardedLevel +
                    "  (" + RewardText(entry.RewardedLevel) + ")");
            }

            return lines;
        }

        /// <summary>หลังจบการต่อสู้: การ์ดที่ใช้ได้ XP (ชนะได้เพิ่ม)</summary>
        public static List<string> ApplyBattle(
            PlayerProfile profile, IEnumerable<string> playedCardIds, bool won)
        {
            List<string> lines = new List<string>();

            if (profile == null || playedCardIds == null)
                return lines;

            foreach (string id in playedCardIds)
                lines.AddRange(AddXp(profile, id, XpPlayed + (won ? XpWon : 0)));

            return lines;
        }
    }
}
