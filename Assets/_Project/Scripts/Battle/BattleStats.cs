using UnityEngine;

namespace MultiverseCardBattle.Battle
{
    /// <summary>
    /// สถิติระหว่างการต่อสู้ ใช้ตรวจเงื่อนไขปลดล็อกการ์ดลับตอนจบแมตช์
    /// ดัชนีของอาร์เรย์คือ PlayerID (1 หรือ 2)
    /// </summary>
    public class BattleStats
    {
        // ชนะการต่อสู้ระหว่างยูนิต: ฝั่งเราโจมตีและยูนิตศัตรูตายโดยตัวเราไม่ตาย
        public readonly int[] CombatWins = new int[3];

        // ยูนิตของเราที่ตายบนสนาม (ไม่นับเนรเทศ)
        public readonly int[] UnitDeaths = new int[3];

        // เอฟเฟกต์ที่ทำลายยูนิตศัตรูทันทีโดยไม่ผ่านการต่อสู้
        public readonly int[] InstantDestroys = new int[3];

        // มียูนิตของเราโจมตีครบ 3 ครั้งโดยยังไม่ตาย
        public readonly bool[] TripleAttacker = new bool[3];

        // จำนวน Spell ที่ใช้
        public readonly int[] SpellsPlayed = new int[3];

        // ยูนิตศัตรูที่ถูกกำจัดด้วยเอฟเฟกต์ (Spell / ความสามารถการ์ด) ไม่ใช่การต่อสู้
        public readonly int[] EffectKills = new int[3];

        // HP Leader ต่ำสุดตลอดแมตช์
        public readonly int[] LowestLeaderHealth = { int.MaxValue, int.MaxValue, int.MaxValue };

        public void Init(BattlePlayer player1, BattlePlayer player2)
        {
            NoteLeaderHealth(player1);
            NoteLeaderHealth(player2);
        }

        public void NoteLeaderHealth(BattlePlayer player)
        {
            if (player == null || player.PlayerID < 1 || player.PlayerID > 2)
                return;

            LowestLeaderHealth[player.PlayerID] = Mathf.Min(
                LowestLeaderHealth[player.PlayerID], player.LeaderHealth);
        }

        private static bool Valid(int playerId)
        {
            return playerId >= 1 && playerId <= 2;
        }

        public void AddCombatWin(int playerId)
        {
            if (Valid(playerId))
                CombatWins[playerId]++;
        }

        public void AddUnitDeath(int playerId)
        {
            if (Valid(playerId))
                UnitDeaths[playerId]++;
        }

        public void NoteSurvivingAttacks(int playerId, int totalAttacks)
        {
            if (Valid(playerId) && totalAttacks >= 3)
                TripleAttacker[playerId] = true;
        }

        public void AddSpellPlayed(int playerId)
        {
            if (Valid(playerId))
                SpellsPlayed[playerId]++;
        }

        public void AddEffectKill(int playerId)
        {
            if (Valid(playerId))
                EffectKills[playerId]++;
        }

        public void AddInstantDestroy(int playerId)
        {
            if (Valid(playerId))
                InstantDestroys[playerId]++;
        }
    }
}
