using System.Collections.Generic;
using MultiverseCardBattle.Battle;
using UnityEngine;

namespace MultiverseCardBattle.Data
{
    /// <summary>ข้อมูล Banner ที่แสดงตอนปลดล็อกการ์ดลับ</summary>
    public class SecretUnlock
    {
        public string CardId;
        public string Title;
        public string Subtitle;
        public Color Tint;
        public BannerStyle Style;
    }

    public enum BannerStyle
    {
        Gold,
        Nazarick,
        Yamato
    }

    /// <summary>
    /// ตรวจเงื่อนไขปลดล็อกการ์ดลับตอนจบแมตช์ (ต้องชนะ และครบทุกข้อในแมตช์เดียว)
    /// </summary>
    public static class SecretCardService
    {
        public const int ArtoriasWins = 8;
        public const int ArtoriasDeaths = 5;
        public const int ArtoriasMinHealth = 15;

        public const int AinzSpells = 10;
        public const int AinzEffectKills = 8;
        public const int AinzMinHealth = 25;

        public static List<SecretUnlock> Evaluate(
            BattleManager battle, int humanId, bool won, PlayerProfile profile)
        {
            List<SecretUnlock> list = new List<SecretUnlock>();

            if (battle == null || profile == null || !won)
                return list;

            if (IsArtoriasEarned(battle.Stats, humanId) &&
                profile.GetOwnedCount(StarterCardLibrary.ArtoriasId) == 0)
            {
                SecretUnlock unlock = new SecretUnlock();

                unlock.CardId = StarterCardLibrary.ArtoriasId;
                unlock.Title = "EVEN IN THE ABYSS, A KNIGHT STILL STANDS";
                unlock.Subtitle = "A Knight's Duty complete";
                unlock.Tint = new Color(0.85f, 0.7f, 0.35f, 1f);

                list.Add(unlock);
            }

            if (IsAinzEarned(battle.Stats, humanId, battle.GetPlayer(humanId)) &&
                profile.GetOwnedCount(StarterCardLibrary.AinzId) == 0)
            {
                SecretUnlock unlock = new SecretUnlock();

                unlock.CardId = StarterCardLibrary.AinzId;
                unlock.Title = "THE SUPREME ONE HAS DESCENDED";
                unlock.Subtitle = "The Throne of Nazarick complete";
                unlock.Tint = new Color(0.9f, 0.12f, 0.16f, 1f);
                unlock.Style = BannerStyle.Nazarick;

                list.Add(unlock);
            }

            if (IsVergilEarned(battle.Stats, humanId) &&
                profile.GetOwnedCount(StarterCardLibrary.VergilId) == 0)
            {
                SecretUnlock unlock = new SecretUnlock();

                unlock.CardId = StarterCardLibrary.VergilId;
                unlock.Title = "THE POWER OF THE YAMATO";
                unlock.Subtitle = "Devil Trigger awakened";
                unlock.Tint = new Color(0.25f, 0.6f, 1f, 1f);
                unlock.Style = BannerStyle.Yamato;

                list.Add(unlock);
            }

            return list;
        }

        // ชนะโดยไม่ใช้ Spell และมียูนิตใบเดียวกันโจมตีครบ 3 ครั้งโดยไม่ตาย
        public static bool IsVergilEarned(BattleStats stats, int humanId)
        {
            if (stats == null)
                return false;

            return stats.TripleAttacker[humanId] &&
                   stats.SpellsPlayed[humanId] == 0;
        }

        public static bool IsAinzEarned(
            BattleStats stats, int humanId, BattlePlayer human)
        {
            if (stats == null || human == null)
                return false;

            return stats.SpellsPlayed[humanId] >= AinzSpells &&
                   stats.EffectKills[humanId] >= AinzEffectKills &&
                   human.LeaderHealth >= AinzMinHealth;
        }

        public static bool IsArtoriasEarned(BattleStats stats, int humanId)
        {
            if (stats == null)
                return false;

            return stats.CombatWins[humanId] >= ArtoriasWins &&
                   stats.UnitDeaths[humanId] >= ArtoriasDeaths &&
                   stats.LowestLeaderHealth[humanId] >= ArtoriasMinHealth &&
                   stats.InstantDestroys[humanId] == 0;
        }
    }
}
