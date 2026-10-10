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
    }

    /// <summary>
    /// ตรวจเงื่อนไขปลดล็อกการ์ดลับตอนจบแมตช์ (ต้องชนะ และครบทุกข้อในแมตช์เดียว)
    /// </summary>
    public static class SecretCardService
    {
        public const int ArtoriasWins = 4;
        public const int ArtoriasDeaths = 3;
        public const int ArtoriasMinHealth = 10;

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

            return list;
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
