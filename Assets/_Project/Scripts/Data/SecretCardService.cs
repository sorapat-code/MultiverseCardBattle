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
        public const int ArtoriasLeaderDamage = 15;
        public const int ArtoriasMaxTurns = 18;

        public const int AinzSpells = 10;
        public const int AinzEffectKills = 8;
        public const int AinzMinHealth = 25;
        public const int AinzUnitsKilled = 12;
        public const int AinzMaxUnitDeaths = 2;
        public const int AinzDistinctSpells = 6;
        public const int AinzMaxTurns = 20;

        public static List<SecretUnlock> Evaluate(
            BattleManager battle, int humanId, bool won, PlayerProfile profile)
        {
            List<SecretUnlock> list = new List<SecretUnlock>();

            if (battle == null || profile == null || !won)
                return list;

            if (IsArtoriasEarned(battle.Stats, humanId, battle.TurnManager.TotalTurns) &&
                profile.GetOwnedCount(StarterCardLibrary.ArtoriasId) == 0)
            {
                SecretUnlock unlock = new SecretUnlock();

                unlock.CardId = StarterCardLibrary.ArtoriasId;
                unlock.Title = "EVEN IN THE ABYSS, A KNIGHT STILL STANDS";
                unlock.Subtitle = "A Knight's Duty complete";
                unlock.Tint = new Color(0.85f, 0.7f, 0.35f, 1f);

                list.Add(unlock);
            }

            if (IsAinzEarned(
                    battle.Stats, humanId, battle.GetPlayer(humanId), battle.TurnManager.TotalTurns) &&
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

        // totalTurns = จำนวนเทิร์นรวมทั้งสองฝ่าย (TurnManager.TotalTurns) ตอนแมตช์จบ
        public static bool IsAinzEarned(
            BattleStats stats, int humanId, BattlePlayer human, int totalTurns)
        {
            if (stats == null || human == null)
                return false;

            return stats.SpellsPlayed[humanId] >= AinzSpells &&
                   stats.DistinctSpells(humanId) >= AinzDistinctSpells &&
                   stats.EffectKills[humanId] >= AinzEffectKills &&
                   stats.UnitsKilled[humanId] >= AinzUnitsKilled &&
                   stats.UnitDeaths[humanId] <= AinzMaxUnitDeaths &&
                   human.LeaderHealth >= AinzMinHealth &&
                   totalTurns <= AinzMaxTurns;
        }

        public static bool IsArtoriasEarned(BattleStats stats, int humanId, int totalTurns)
        {
            if (stats == null)
                return false;

            return stats.CombatWins[humanId] >= ArtoriasWins &&
                   stats.UnitDeaths[humanId] >= ArtoriasDeaths &&
                   stats.LowestLeaderHealth[humanId] >= ArtoriasMinHealth &&
                   stats.InstantDestroys[humanId] == 0 &&
                   stats.SpellsPlayed[humanId] == 0 &&
                   stats.LeaderDamage[humanId] >= ArtoriasLeaderDamage &&
                   totalTurns <= ArtoriasMaxTurns;
        }
    }
}
