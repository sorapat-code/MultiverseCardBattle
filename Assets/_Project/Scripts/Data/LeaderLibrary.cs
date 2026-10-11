using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// ข้อมูล Leader หนึ่งตัว: สกิลเฉพาะตัวที่กดใช้ได้ (เทิร์นละครั้ง) และเงื่อนไขปลดล็อกการ์ดไม้ตาย
    /// ไม่มีสกิลติดตัว (ตัดออกตามดีไซน์)
    ///
    /// ตอนนี้เงื่อนไขของทุก Leader เป็นมิเตอร์ "Power" ที่สะสมจากการกระทำต่างๆ (ดู LeaderSystem)
    /// ครบแล้วการ์ดไม้ตายเข้ามือครั้งเดียวต่อแมตช์
    /// </summary>
    /// <summary>สกิลกดของ Leader (LeaderSkillCommand แยกการทำงานตามค่านี้)</summary>
    public enum LeaderSkillKind
    {
        Kaioken,     // Goku: ยูนิตทุกตัว +ATK จนจบเทิร์น (จ่ายเลือด Leader)
        ShadowClone, // Naruto: เรียก Shadow Clone
        Infinity,    // Gojo: ยูนิตทุกตัวได้ Barrier
        Repulsor     // Tony: ยิง 1 ดาเมจใส่ศัตรูทุกตัวและ Leader ศัตรู
    }

    /// <summary>เหตุการณ์ที่ทำให้มิเตอร์ Power เพิ่ม +1 (รวมหลายอย่างได้)</summary>
    [System.Flags]
    public enum PowerGain
    {
        None = 0,
        LeaderDamaged = 1,  // Leader ของเราโดนดาเมจ
        UnitDied = 2,       // ยูนิตของเราตาย
        UnitPlayed = 4,     // เราลงยูนิตจากมือ
        SpellPlayed = 8,    // เราใช้ Spell
        EnemyUnitDied = 16, // ยูนิตศัตรูตาย
        SkillUsed = 32      // เรากดสกิล
    }

    public class LeaderData
    {
        public string Id;
        public string Name;
        public string Title;
        public Universe Universe;

        public Color Theme;

        // สกิลกด
        public LeaderSkillKind SkillKind;
        public string SkillName;
        public string SkillText;
        public int SkillEnergyCost;
        public int SkillHealthCost;

        // เงื่อนไขไม้ตาย
        public string PowerName;
        public int PowerMax;
        public PowerGain PowerGain;
        public string PowerText;

        public string FinisherId;

        /// <summary>ข้อความต้นทุนสกิล เช่น "1 Energy, -2 HP"</summary>
        public string SkillCostText
        {
            get
            {
                string text = SkillEnergyCost + " Energy";

                if (SkillHealthCost > 0)
                    text += ", -" + SkillHealthCost + " HP";

                return text;
            }
        }
    }

    public static class LeaderLibrary
    {
        public const string GokuId = "LDR_001";
        public const string NarutoId = "LDR_002";
        public const string GojoId = "LDR_003";
        public const string TonyId = "LDR_004";

        private static List<LeaderData> all;

        public static IReadOnlyList<LeaderData> All
        {
            get
            {
                Build();

                return all;
            }
        }

        public static LeaderData Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            Build();

            foreach (LeaderData leader in all)
            {
                if (leader.Id == id)
                    return leader;
            }

            return null;
        }

        private static void Build()
        {
            if (all != null)
                return;

            all = new List<LeaderData>();

            all.Add(new LeaderData
            {
                Id = GokuId,
                Name = "Son Goku",
                Title = "Saiyan Raised on Earth",
                Universe = Universe.DragonBall,
                Theme = new Color(1f, 0.6f, 0.15f, 1f),

                SkillKind = LeaderSkillKind.Kaioken,
                SkillName = "Kaioken",
                SkillEnergyCost = 1,
                SkillHealthCost = 2,
                SkillText =
                    "Once per turn: pay 1 Energy and 2 of your Leader's Health. " +
                    "All your units gain +2 Attack until end of turn.",

                PowerName = "Ki",
                PowerMax = 10,
                PowerGain = PowerGain.LeaderDamaged | PowerGain.UnitDied | PowerGain.SkillUsed,
                PowerText =
                    "Gain Ki: +1 whenever your Leader takes damage, +1 whenever one of your units dies, " +
                    "+1 each time you use Kaioken. At 10 Ki, Spirit Bomb joins your hand (once per match).",

                FinisherId = StarterCardLibrary.SpiritBombId
            });

            all.Add(new LeaderData
            {
                Id = NarutoId,
                Name = "Naruto Uzumaki",
                Title = "Hokage-to-be of the Leaf",
                Universe = Universe.Naruto,
                Theme = new Color(1f, 0.55f, 0.1f, 1f),

                SkillKind = LeaderSkillKind.ShadowClone,
                SkillName = "Shadow Clone",
                SkillEnergyCost = 2,
                SkillHealthCost = 0,
                SkillText =
                    "Once per turn: pay 2 Energy. Summon two 1/1 Shadow Clones " +
                    "(as many as fit on your field).",

                PowerName = "Chakra",
                PowerMax = 8,
                PowerGain = PowerGain.UnitPlayed | PowerGain.SkillUsed,
                PowerText =
                    "Gain Chakra: +1 whenever you play a unit from your hand, +1 each time you use " +
                    "Shadow Clone. At 8 Chakra, Tailed Beast Bomb joins your hand (once per match).",

                FinisherId = StarterCardLibrary.TailedBeastBombId
            });

            all.Add(new LeaderData
            {
                Id = GojoId,
                Name = "Satoru Gojo",
                Title = "The Strongest Sorcerer",
                Universe = Universe.JujutsuKaisen,
                Theme = new Color(0.45f, 0.7f, 1f, 1f),

                SkillKind = LeaderSkillKind.Infinity,
                SkillName = "Infinity",
                SkillEnergyCost = 2,
                SkillHealthCost = 0,
                SkillText =
                    "Once per turn: pay 2 Energy. All your units gain a Barrier that absorbs 3 damage " +
                    "until the end of your next turn.",

                PowerName = "Cursed Energy",
                PowerMax = 8,
                PowerGain = PowerGain.SpellPlayed | PowerGain.SkillUsed,
                PowerText =
                    "Gain Cursed Energy: +1 whenever you play a spell, +1 each time you use Infinity. " +
                    "At 8, Hollow Purple joins your hand (once per match).",

                FinisherId = StarterCardLibrary.HollowPurpleId
            });

            all.Add(new LeaderData
            {
                Id = TonyId,
                Name = "Tony Stark",
                Title = "Genius, Billionaire, Iron Man",
                Universe = Universe.Original,
                Theme = new Color(1f, 0.35f, 0.25f, 1f),

                SkillKind = LeaderSkillKind.Repulsor,
                SkillName = "Repulsor",
                SkillEnergyCost = 2,
                SkillHealthCost = 0,
                SkillText =
                    "Once per turn: pay 2 Energy. Deal 1 damage to every enemy unit and to the enemy Leader.",

                PowerName = "Arc Reactor",
                PowerMax = 8,
                PowerGain = PowerGain.SpellPlayed | PowerGain.EnemyUnitDied,
                PowerText =
                    "Charge the Arc Reactor: +1 whenever you play a spell, +1 whenever an enemy unit dies. " +
                    "At 8, Proton Cannon joins your hand (once per match).",

                FinisherId = StarterCardLibrary.ProtonCannonId
            });
        }
    }
}
