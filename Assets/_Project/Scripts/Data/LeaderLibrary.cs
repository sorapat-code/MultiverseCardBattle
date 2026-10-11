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
    public class LeaderData
    {
        public string Id;
        public string Name;
        public string Title;
        public Universe Universe;

        public Color Theme;

        // สกิลกด
        public string SkillName;
        public string SkillText;
        public int SkillEnergyCost;
        public int SkillHealthCost;

        // เงื่อนไขไม้ตาย
        public string PowerName;
        public int PowerMax;
        public string PowerText;

        public string FinisherId;
    }

    public static class LeaderLibrary
    {
        public const string GokuId = "LDR_001";

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

                SkillName = "Kaioken",
                SkillEnergyCost = 1,
                SkillHealthCost = 2,
                SkillText =
                    "Once per turn: pay 1 Energy and 2 of your Leader's Health. " +
                    "All your units gain +2 Attack until end of turn.",

                PowerName = "Ki",
                PowerMax = 10,
                PowerText =
                    "Gain Ki: +1 whenever your Leader takes damage, +1 whenever one of your units dies, " +
                    "+1 each time you use Kaioken. At 10 Ki, Spirit Bomb joins your hand (once per match).",

                FinisherId = StarterCardLibrary.SpiritBombId
            });
        }
    }
}
