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
    /// <summary>
    /// ชนิดสกิลกดของ Leader (LeaderSkillCommand แยกการทำงานตามค่านี้)
    /// ใช้ SkillValue เป็นตัวเลขหลักของสกิล
    /// </summary>
    public enum LeaderSkillKind
    {
        BuffAttack,     // ยูนิตทุกตัวของเรา +SkillValue Attack จนจบเทิร์น
        Barrier,        // ยูนิตทุกตัวของเราได้ Barrier SkillValue จนจบเทิร์นหน้าของเรา
        Heal,           // รักษา Leader และยูนิตทุกตัวของเรา SkillValue
        SummonTokens,   // เรียก SkillTokenId จำนวน SkillCount ตัว
        DamageEnemies,  // ดาเมจ SkillValue ใส่ยูนิตศัตรูทุกตัว (และ Leader ศัตรูถ้า SkillHitsLeader)
        WeakenEnemies   // ยูนิตศัตรูทุกตัวสร้างดาเมจลดลงครึ่งหนึ่งจนจบเทิร์นหน้าของศัตรู
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
        public int SkillValue;
        public int SkillCount;
        public string SkillTokenId;
        public bool SkillHitsLeader;
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

        // false = Leader ของบอสในโหมดเนื้อเรื่อง (ผู้เล่นเลือกไม่ได้)
        public bool PlayerSelectable = true;

        // ข้อความสั้นๆ บอกคาแรกเตอร์ของ Leader (ใช้ตอนแนะนำบอส)
        public string Flavor;

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

        // บอสท้ายบทของโหมดเนื้อเรื่อง (ศัตรูเท่านั้น)
        public const string BanditChiefId = "BOSS_01";
        public const string HighPriestessId = "BOSS_02";
        public const string GatekeeperId = "BOSS_03";
        public const string PirateEmperorId = "BOSS_04";
        public const string SovereignId = "BOSS_05";

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

                SkillKind = LeaderSkillKind.BuffAttack,
                SkillValue = 2,
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

                SkillKind = LeaderSkillKind.SummonTokens,
                SkillCount = 2,
                SkillTokenId = StarterCardLibrary.ShadowCloneId,
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

                SkillKind = LeaderSkillKind.Barrier,
                SkillValue = 3,
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

                SkillKind = LeaderSkillKind.DamageEnemies,
                SkillValue = 1,
                SkillHitsLeader = true,
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

            // ---------- บอสท้ายบท (ศัตรูในโหมดเนื้อเรื่อง) ----------

            all.Add(new LeaderData
            {
                Id = BanditChiefId,
                Name = "Bandit Chief",
                Title = "Terror of Bandit Road",
                Flavor = "Fast raiders. Every unit he sends in hits harder after his orders.",
                Universe = Universe.Original,
                Theme = new Color(0.85f, 0.55f, 0.2f, 1f),
                PlayerSelectable = false,

                SkillKind = LeaderSkillKind.BuffAttack,
                SkillValue = 1,
                SkillName = "Raid Orders",
                SkillEnergyCost = 1,
                SkillText = "Once per turn: pay 1 Energy. All the Chief's units gain +1 Attack until end of turn.",

                PowerName = "Loot",
                PowerMax = 6,
                PowerGain = PowerGain.UnitPlayed | PowerGain.SkillUsed,
                PowerText = "+1 whenever he plays a unit, +1 each time he uses Raid Orders. At 6 Loot, Ambush! joins his hand.",

                FinisherId = StarterCardLibrary.AmbushId
            });

            all.Add(new LeaderData
            {
                Id = HighPriestessId,
                Name = "High Priestess",
                Title = "Keeper of the Temple Wall",
                Flavor = "Heals and endures. Hurting her Leader only feeds her faith.",
                Universe = Universe.Original,
                Theme = new Color(1f, 0.95f, 0.6f, 1f),
                PlayerSelectable = false,

                SkillKind = LeaderSkillKind.Heal,
                SkillValue = 2,
                SkillName = "Sanctuary Prayer",
                SkillEnergyCost = 2,
                SkillText = "Once per turn: pay 2 Energy. Heal her Leader and all her units for 2.",

                PowerName = "Faith",
                PowerMax = 6,
                PowerGain = PowerGain.LeaderDamaged | PowerGain.SkillUsed,
                PowerText = "+1 whenever her Leader takes damage, +1 each time she prays. At 6 Faith, Divine Judgment joins her hand.",

                FinisherId = StarterCardLibrary.DivineJudgmentId
            });

            all.Add(new LeaderData
            {
                Id = GatekeeperId,
                Name = "Gatekeeper",
                Title = "Warden of the Titan Gate",
                Flavor = "Sweeping stomps punish wide boards. Every fallen titan fuels the next blow.",
                Universe = Universe.Original,
                Theme = new Color(0.7f, 0.7f, 0.75f, 1f),
                PlayerSelectable = false,

                SkillKind = LeaderSkillKind.DamageEnemies,
                SkillValue = 2,
                SkillHitsLeader = false,
                SkillName = "Titan Stomp",
                SkillEnergyCost = 3,
                SkillText = "Once per turn: pay 3 Energy. Deal 2 damage to all your units.",

                PowerName = "Fortitude",
                PowerMax = 6,
                PowerGain = PowerGain.UnitDied | PowerGain.LeaderDamaged,
                PowerText = "+1 whenever his Leader takes damage, +1 whenever one of his units dies. At 6, Gate Collapse joins his hand.",

                FinisherId = StarterCardLibrary.GateCollapseId
            });

            all.Add(new LeaderData
            {
                Id = PirateEmperorId,
                Name = "Pirate Emperor",
                Title = "Ruler of the Grand Line",
                Flavor = "His presence alone cuts your units' strength in half.",
                Universe = Universe.Original,
                Theme = new Color(0.9f, 0.25f, 0.2f, 1f),
                PlayerSelectable = false,

                SkillKind = LeaderSkillKind.WeakenEnemies,
                SkillName = "Conqueror's Haki",
                SkillEnergyCost = 2,
                SkillText = "Once per turn: pay 2 Energy. All your units deal half damage until the end of your next turn.",

                PowerName = "Haki",
                PowerMax = 7,
                PowerGain = PowerGain.LeaderDamaged | PowerGain.UnitPlayed | PowerGain.SkillUsed,
                PowerText = "+1 whenever his Leader takes damage, +1 per unit played, +1 per Haki burst. At 7, Emperor's Wrath joins his hand.",

                FinisherId = StarterCardLibrary.EmperorWrathId
            });

            all.Add(new LeaderData
            {
                Id = SovereignId,
                Name = "The Multiverse Sovereign",
                Title = "Ruler of Fractured Worlds",
                Flavor = "Calls the echoes of fallen heroes. When enough fall, the worlds are rewritten.",
                Universe = Universe.Original,
                Theme = new Color(0.7f, 0.4f, 1f, 1f),
                PlayerSelectable = false,

                SkillKind = LeaderSkillKind.SummonTokens,
                SkillCount = 2,
                SkillTokenId = StarterCardLibrary.HeroEchoId,
                SkillName = "Echo of Heroes",
                SkillEnergyCost = 3,
                SkillText = "Once per turn: pay 3 Energy. Summon two 2/2 Hero Echoes.",

                PowerName = "Fracture",
                PowerMax = 8,
                PowerGain = PowerGain.UnitDied | PowerGain.EnemyUnitDied | PowerGain.SkillUsed,
                PowerText = "+1 whenever any unit of his dies, +1 whenever one of yours dies, +1 per Echo call. At 8 Fracture, World Rewrite joins his hand.",

                FinisherId = StarterCardLibrary.WorldRewriteId
            });
        }
    }
}
