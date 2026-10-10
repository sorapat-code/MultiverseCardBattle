using System.Collections.Generic;
using UnityEngine;
using MultiverseCardBattle.Cards;
using MultiverseCardBattle.Conditions;
using MultiverseCardBattle.Effects;
using MultiverseCardBattle.Targeting;

namespace MultiverseCardBattle.Data
{
    /// <summary>
    /// การ์ดตั้งต้นที่สร้างจากโค้ด (ประกอบจาก Trigger / Target / Effect ที่มีอยู่)
    /// ทำให้เกมเล่นได้ทันทีโดยไม่ต้องสร้าง asset และใช้เป็นตัวอย่างการประกอบการ์ด
    /// ส่งออกเป็น asset ได้จากเมนู Multiverse Card Battle > Export Starter Cards
    /// </summary>
    public static class StarterCardLibrary
    {
        private static List<CardData> cards;
        private static Dictionary<string, CardData> byId;

        public static IReadOnlyList<CardData> All
        {
            get
            {
                Build();
                return cards;
            }
        }

        public static CardData Get(string cardId)
        {
            Build();

            CardData data;

            if (byId.TryGetValue(cardId, out data))
                return data;

            return null;
        }

        // การ์ดที่ผู้เล่นฝั่งเริ่มหลังได้ตอนโยนเหรียญ: +1 Energy ในเทิร์นนั้น
        public const string CoinId = "TKN_COIN";

        // การ์ดลับ: มีโอกาสน้อยมากที่ Summon ในการต่อสู้จะได้การ์ดนี้แทน Token
        public const string SecretId = "EVT_001";

        // การ์ดลับที่ปลดล็อกจากเงื่อนไขในแมตช์ (SecretCardService)
        public const string ArtoriasId = "EVT_002";

        private static void Build()
        {
            if (cards != null)
                return;

            cards = new List<CardData>();
            byId = new Dictionary<string, CardData>();

            // ---------- Token ----------

            CardData wisp = Add(Unit(
                "TKN_001", "Spirit Wisp",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                1, 1, 1, Keyword.None,
                CardTag.Spirit | CardTag.Token,
                CardSource.Token));

            Add(Create(
                CoinId, "The Coin", CardType.Spell,
                Universe.Original, Faction.Neutral, CardRarity.Common,
                0, 0, 0, Keyword.None,
                CardTag.Magic | CardTag.Token,
                CardSource.Token,
                new[]
                {
                    Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                        Fx(EffectType.GainEnergy, 1))
                }));

            CardData clone = Add(Unit(
                "TKN_002", "Shadow Clone",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 1, 1, Keyword.None,
                CardTag.Human | CardTag.Token,
                CardSource.Token));


            // ร่างแปลงของ Goku (Transformation ชั่วคราว)
            CardData superGoku = Add(Unit(
                "TKN_003", "Super Saiyan Goku",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                8, 7, 8, Keyword.DoubleStrike | Keyword.Unstoppable,
                CardTag.Human | CardTag.Elite | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            // ร่างวิวัฒนาการของ Ichigo (Evolve)
            CardData bankai = Add(Unit(
                "TKN_004", "Ichigo, Bankai",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 7, 7, Keyword.QuickAttack | Keyword.Pierce,
                CardTag.Human | CardTag.Spirit | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            CardData shadowSoldier = Add(Unit(
                "TKN_005", "Shadow Soldier",
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Rare,
                2, 2, 2, Keyword.Rush,
                CardTag.Undead | CardTag.Melee | CardTag.Token,
                CardSource.Token));

            // การ์ดลับ (ไม่อยู่ใน Collection ได้จากโอกาส 1% ตอน Summon เท่านั้น)
            Add(Create(
                SecretId, "Sung Jin-Woo, Shadow Monarch", CardType.Unit,
                Universe.SoloLeveling, Faction.ShadowArmy, CardRarity.Secret,
                9, 8, 8, Keyword.Charge | Keyword.Lifesteal | Keyword.Shadowbound | Keyword.Unique,
                CardTag.Human | CardTag.Elite | CardTag.Boss | CardTag.Dark,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Summon, TargetRule.AllEnemies, TargetSelection.All,
                        Fx(EffectType.DealDamage, 3)),
                    Act(TriggerType.Summon, TargetRule.AllyLeader, TargetSelection.All,
                        FxCard(EffectType.Summon, shadowSoldier),
                        FxCard(EffectType.Summon, shadowSoldier)),
                    Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                        FxCard(EffectType.Summon, shadowSoldier))
                }));

            // Artorias: ปลดล็อกด้วยเงื่อนไข "A Knight's Duty" ไม่ใช่การสุ่ม
            CardData artorias = Create(
                ArtoriasId, "Artorias the Abysswalker", CardType.Unit,
                Universe.DarkSouls, Faction.Lordran, CardRarity.Secret,
                7, 6, 9, Keyword.Taunt | Keyword.Lifesteal | Keyword.Unique,
                CardTag.Human | CardTag.Elite | CardTag.Boss | CardTag.Melee | CardTag.Tank,
                CardSource.Event,
                new[]
                {
                    Act(TriggerType.Summon, TargetRule.AllEnemies, TargetSelection.All,
                        FxHeal(EffectType.DealDamage, 2, 2))
                });

            artorias.Passives = CardPassive.Abysswalker | CardPassive.LastGuardian;
            artorias.Description =
                "Taunt, Lifesteal, Unique\n" +
                "Summon: Deal 2 damage to all enemy units. Restore 2 HP to your Leader for each enemy that took damage.\n" +
                "Abysswalker: Whenever damaged by an attack, +1 ATK until end of turn.\n" +
                "Last Guardian: Once per match, instead of dying, survive with 1 HP but lose Taunt until the end of next turn.";

            Add(artorias);

            // ---------- Units ----------

            Add(Unit("STR_001", "Leaf Genin",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Common,
                1, 1, 2, Keyword.None, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_002", "Shield Guardian",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 1, 4, Keyword.Taunt, CardTag.Human | CardTag.Tank));

            Add(Unit("STR_003", "Field Medic",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                2, 2, 2, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 4))));

            Add(Unit("STR_004", "Pirate Sharpshooter",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Common,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Ranged,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("STR_005", "Iron Golem",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                4, 3, 6, Keyword.Taunt, CardTag.Machine | CardTag.Tank));

            Add(Unit("STR_006", "Mist Assassin",
                Universe.Naruto, Faction.MistVillage, CardRarity.Common,
                3, 3, 2, Keyword.Stealth, CardTag.Human | CardTag.Assassin));

            Add(Unit("STR_007", "Vampire Knight",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                3, 2, 4, Keyword.Lifesteal, CardTag.Undead | CardTag.Melee));

            Add(Unit("STR_008", "Disaster Curse",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Rare,
                2, 1, 2, Keyword.Poison, CardTag.Spirit | CardTag.Demon));

            Add(Unit("STR_009", "Megumi Fushiguro",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                4, 3, 3, Keyword.Shield, CardTag.Human | CardTag.Magic | CardTag.Melee));

            Add(Unit("STR_010", "Soul Society Scout",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Common,
                3, 3, 1, Keyword.Charge, CardTag.Spirit | CardTag.Melee));

            Add(Unit("STR_011", "Twin Blade Duelist",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 3, 3, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_012", "Raid Skirmisher",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 3, 2, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("STR_013", "Sage Scholar",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 1, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Unit("STR_014", "Nobara Kugisaki",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Common,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 1))));

            Add(Unit("STR_015", "Death Cultist",
                Universe.Original, Faction.Neutral, CardRarity.Common,
                3, 2, 2, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Death, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("STR_016", "Spirit Caller",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                4, 2, 3, Keyword.None, CardTag.Spirit | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, wisp),
                    FxCard(EffectType.Summon, wisp))));

            Add(Unit("STR_017", "Pyromancer",
                Universe.Original, Faction.Neutral, CardRarity.Rare,
                4, 3, 4, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Burn, 2, 3))));

            Add(Unit("STR_018", "Toge Inumaki",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("STR_019", "Silencer Monk",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Rare,
                3, 3, 2, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.Silence))));

            Add(Unit("STR_020", "Jogo, Volcano Curse",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Legendary,
                7, 6, 7, Keyword.None, CardTag.Demon | CardTag.Boss | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Unit("STR_021", "Titan Colossus",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                6, 6, 6, Keyword.Taunt, CardTag.Machine | CardTag.Elite));

            Add(Unit("STR_022", "Shoko Ieiri",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Common,
                3, 2, 3, Keyword.None, CardTag.Human | CardTag.Support,
                Act(TriggerType.TurnStart, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 2))));

            Add(Unit("STR_023", "Berserker",
                Universe.Fate, Faction.Chaldea, CardRarity.Common,
                2, 2, 3, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 1))));

            Add(Unit("STR_024", "Gladiator",
                Universe.Original, Faction.Neutral, CardRarity.Epic,
                5, 5, 5, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee));

            // ---------- Multiverse Heroes ----------
            // ตัวละครจากหลายจักรวาล ประกอบจาก Trigger / Target / Effect เดิมทั้งหมด

            Add(Unit("NAR_001", "Naruto Uzumaki",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Epic,
                5, 3, 4, Keyword.None, CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    FxCard(EffectType.Summon, clone),
                    FxCard(EffectType.Summon, clone))));

            Add(Unit("NAR_002", "Sasuke Uchiha",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Rare,
                4, 4, 3, Keyword.Charge, CardTag.Human | CardTag.Assassin | CardTag.Lightning));

            Add(Unit("OPC_001", "Monkey D. Luffy",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Epic,
                5, 4, 6, Keyword.Rush, CardTag.Human | CardTag.Melee));

            Add(Unit("OPC_002", "Roronoa Zoro",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                5, 3, 5, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("BLE_001", "Ichigo Kurosaki",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Epic,
                6, 5, 5, Keyword.Charge, CardTag.Human | CardTag.Spirit | CardTag.Melee));

            Add(Unit("BLE_002", "Rukia Kuchiki",
                Universe.Bleach, Faction.Gotei13, CardRarity.Rare,
                3, 2, 3, Keyword.None, CardTag.Spirit | CardTag.Magic | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("DBZ_001", "Son Goku",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                8, 7, 7, Keyword.Charge, CardTag.Human | CardTag.Elite | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            Add(Unit("DBZ_002", "Vegeta",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                6, 5, 5, Keyword.None, CardTag.Human | CardTag.Elite | CardTag.Melee,
                Act(TriggerType.DamageTaken, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.GainAttack, 2))));

            Add(Unit("JJB_001", "Jotaro Kujo",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Epic,
                6, 4, 5, Keyword.DoubleStrike, CardTag.Human | CardTag.Melee));

            Add(Unit("JJB_002", "Dio Brando",
                Universe.JoJo, Faction.DioFollowers, CardRarity.Legendary,
                7, 6, 6, Keyword.Lifesteal, CardTag.Undead | CardTag.Elite,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.All,
                    FxStatus(EffectType.Stun, 0, 1))));

            Add(Unit("FAT_001", "Artoria Pendragon",
                Universe.Fate, Faction.Chaldea, CardRarity.Legendary,
                6, 5, 5, Keyword.Shield, CardTag.Human | CardTag.Melee | CardTag.Light,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5))));

            Add(Unit("HSR_001", "March 7th",
                Universe.HonkaiStarRail, Faction.AstralExpress, CardRarity.Common,
                3, 2, 3, Keyword.Taunt, CardTag.Human | CardTag.Ranged | CardTag.Ice,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield))));

            Add(Unit("HSR_002", "Kafka",
                Universe.HonkaiStarRail, Faction.StellaronHunters, CardRarity.Epic,
                4, 3, 3, Keyword.None, CardTag.Human | CardTag.Magic | CardTag.Lightning,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.All,
                    FxStatus(EffectType.Burn, 1, 3))));

            Add(Unit("WUW_001", "Jiyan",
                Universe.WutheringWaves, Faction.Jinzhou, CardRarity.Rare,
                5, 4, 4, Keyword.Rush, CardTag.Human | CardTag.Melee | CardTag.Wind,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("MYT_001", "Satoru Gojo",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Legendary,
                9, 7, 7, Keyword.Shield, CardTag.Human | CardTag.Magic | CardTag.Boss,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 4))));

            Add(Unit("MYT_002", "Ryomen Sukuna",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Legendary,
                7, 5, 6, Keyword.None, CardTag.Demon | CardTag.Boss | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DestroyCard))));


            // ---------- Keyword / Status Showcase ----------
            // การ์ดที่ใช้ Keyword และสถานะใหม่ (Pierce, Quick Attack, Aura, Evolve ฯลฯ)

            Add(Unit("NAR_003", "Rock Lee",
                Universe.Naruto, Faction.LeafVillage, CardRarity.Rare,
                3, 3, 2, Keyword.Rush | Keyword.QuickAttack,
                CardTag.Human | CardTag.Melee));

            Add(Unit("NAR_004", "Itachi Uchiha",
                Universe.Naruto, Faction.Akatsuki, CardRarity.Epic,
                5, 4, 4, Keyword.Stealth,
                CardTag.Human | CardTag.Assassin | CardTag.Dark,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Confusion, 0, 2))));

            Add(Unit("OPC_003", "Sanji",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 3, 3, Keyword.None,
                CardTag.Human | CardTag.Melee | CardTag.Fire,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 1)),
                ActIf(ConditionType.Combo, 1,
                    TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2))));

            Add(Unit("OPC_004", "Jinbe",
                Universe.OnePiece, Faction.StrawHatPirates, CardRarity.Rare,
                4, 3, 6, Keyword.Guard,
                CardTag.Human | CardTag.Tank,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    FxApply(StatusType.DefenseUp, 2, 3))));

            CardData shikai = Add(Unit("BLE_003", "Ichigo, Shikai",
                Universe.Bleach, Faction.SoulSociety, CardRarity.Rare,
                4, 3, 4, Keyword.QuickAttack,
                CardTag.Human | CardTag.Spirit | CardTag.Melee));

            // Evolve: อยู่บนสนามครบ 2 เทิร์นแล้วปลดปล่อย Bankai
            shikai.EvolvesInto = bankai;
            shikai.EvolveAfterTurns = 2;

            Add(Unit("BLE_004", "Kenpachi Zaraki",
                Universe.Bleach, Faction.Gotei13, CardRarity.Epic,
                6, 6, 5, Keyword.Unstoppable | Keyword.Rush,
                CardTag.Human | CardTag.Elite | CardTag.Melee));

            Add(Unit("DBZ_003", "Goku, Saiyan Warrior",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Epic,
                5, 4, 5, Keyword.None,
                CardTag.Human | CardTag.Elite | CardTag.Melee,
                ActIf(ConditionType.SelfHealthLessThan, 15,
                    TriggerType.TurnStart, TargetRule.Self, TargetSelection.All,
                    FxTransform(superGoku, 3))));

            Add(Unit("DBZ_004", "Frieza",
                Universe.DragonBall, Faction.FriezaForce, CardRarity.Epic,
                7, 5, 6, Keyword.None,
                CardTag.Elite | CardTag.Boss | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    FxStatus(EffectType.Burn, 1, 3))));

            Add(Unit("DBZ_005", "Whis",
                Universe.DragonBall, Faction.ZFighters, CardRarity.Legendary,
                8, 3, 8, Keyword.Immune | Keyword.Guard,
                CardTag.God | CardTag.Elite | CardTag.Support));

            Add(Unit("JJB_003", "Star Platinum",
                Universe.JoJo, Faction.StardustCrusaders, CardRarity.Epic,
                7, 6, 5, Keyword.QuickAttack | Keyword.Overwhelm,
                CardTag.Spirit | CardTag.Melee));

            CardData gilgamesh = Add(Unit("FAT_002", "Gilgamesh",
                Universe.Fate, Faction.Chaldea, CardRarity.Legendary,
                8, 6, 6, Keyword.Ward,
                CardTag.Human | CardTag.Elite | CardTag.Light));

            // Aura: ยูนิตพวกเดียวกันตัวอื่นได้ +1 Attack ขณะ Gilgamesh อยู่บนสนาม
            gilgamesh.AuraAttack = 1;

            Add(Unit("FAT_003", "Medea, Witch of Betrayal",
                Universe.Fate, Faction.Chaldea, CardRarity.Rare,
                3, 2, 3, Keyword.Spellburst,
                CardTag.Human | CardTag.Magic,
                Act(TriggerType.Spellburst, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Unit("HSR_003", "Dan Heng",
                Universe.HonkaiStarRail, Faction.AstralExpress, CardRarity.Rare,
                3, 3, 3, Keyword.Pierce,
                CardTag.Human | CardTag.Ranged | CardTag.Wind));

            Add(Unit("HSR_004", "Silver Wolf",
                Universe.HonkaiStarRail, Faction.StellaronHunters, CardRarity.Epic,
                4, 2, 4, Keyword.None,
                CardTag.Human | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Vulnerable, 0, 3),
                    FxApply(StatusType.AttackDown, 2, 3))));

            Add(Unit("WUW_002", "Rover, Resonator",
                Universe.WutheringWaves, Faction.BlackShores, CardRarity.Epic,
                4, 3, 3, Keyword.None,
                CardTag.Human | CardTag.Melee,
                Act(TriggerType.Play, TargetRule.Self, TargetSelection.All,
                    Fx(EffectType.Adapt, 3))));

            Add(Unit("MYT_003", "Yuji Itadori",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Epic,
                5, 4, 3, Keyword.Reborn,
                CardTag.Human | CardTag.Melee));

            Add(Unit("MYT_004", "Principal Yaga",
                Universe.JujutsuKaisen, Faction.JujutsuHigh, CardRarity.Rare,
                4, 2, 5, Keyword.Ward,
                CardTag.Human | CardTag.Support,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Barrier, 2, 0))));

            Add(Unit("MYT_005", "Mahito",
                Universe.JujutsuKaisen, Faction.CursedSpirits, CardRarity.Rare,
                4, 3, 3, Keyword.None,
                CardTag.Spirit | CardTag.Demon | CardTag.Magic,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Disarm, 0, 2),
                    FxApply(StatusType.Poison, 1, 3))));

            // ---------- Spells ----------

            Add(Spell("SPL_001", "Lightning Bolt", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.EnemyLeader, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Spell("SPL_002", "Fireball", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 4))));

            Add(Spell("SPL_003", "Healing Light", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Heal, 5))));

            Add(Spell("SPL_004", "Power Surge", CardRarity.Common, 1,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.GainAttack, 2),
                    Fx(EffectType.GainHealth, 2))));

            Add(Spell("SPL_005", "Arcane Insight", CardRarity.Common, 3,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 2))));

            Add(Spell("SPL_006", "Mass Smite", CardRarity.Epic, 5,
                Act(TriggerType.Play, TargetRule.AllEnemies, TargetSelection.All,
                    Fx(EffectType.DealDamage, 3))));

            Add(Spell("SPL_007", "Assassinate", CardRarity.Rare, 4,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DestroyCard))));

            Add(Spell("SPL_008", "Recall", CardRarity.Rare, 1,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    Fx(EffectType.ReturnToHand),
                    Fx(EffectType.ReduceCost, 1))));

            Add(Spell("SPL_009", "Discovery Scroll", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.Discover, 3))));

            Add(Spell("SPL_010", "Divine Blessing", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.AllyUnit, TargetSelection.Chosen,
                    FxKeyword(EffectType.AddKeyword, Keyword.Shield))));

            Add(Spell("SPL_011", "Hex of Ruin", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Weakness, 0, 3),
                    FxApply(StatusType.Curse, 0, 3),
                    FxApply(StatusType.Marked, 0, 3))));

            Add(Spell("SPL_012", "Frozen Prison", CardRarity.Rare, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    FxApply(StatusType.Freeze, 0, 2),
                    FxApply(StatusType.Root, 0, 2))));

            Add(Spell("SPL_013", "Rejuvenation", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxStatus(EffectType.Regen, 2, 3),
                    FxApply(StatusType.Blessed, 0, 3))));

            Add(Spell("SPL_014", "Opportunist", CardRarity.Common, 2,
                Act(TriggerType.Play, TargetRule.EnemyUnit, TargetSelection.Chosen,
                    Fx(EffectType.DealDamage, 2)),
                ActIf(ConditionType.Combo, 1,
                    TriggerType.Play, TargetRule.AllyLeader, TargetSelection.All,
                    Fx(EffectType.DrawCard, 1))));

            Add(Spell("SPL_015", "Sanctuary", CardRarity.Rare, 3,
                Act(TriggerType.Play, TargetRule.AllAllies, TargetSelection.All,
                    FxApply(StatusType.Protected, 0, 2),
                    FxApply(StatusType.DefenseUp, 1, 2))));
        }

        // ---------- ตัวช่วยสร้างการ์ด ----------

        private static CardData Add(CardData data)
        {
            cards.Add(data);
            byId[data.CardID] = data;

            return data;
        }

        private static CardData Create(
            string id,
            string name,
            CardType type,
            Universe universe,
            Faction faction,
            CardRarity rarity,
            int cost,
            int attack,
            int health,
            Keyword keywords,
            CardTag tags,
            CardSource source,
            CardActionData[] actions)
        {
            CardData data = ScriptableObject.CreateInstance<CardData>();

            // ป้องกัน Unity เก็บกวาด asset ที่สร้างจากโค้ดตอนเปลี่ยนซีน
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;

            data.name = id;
            data.CardID = id;
            data.CardName = name;
            data.Version = "1.0";
            data.Description = "";

            data.Type = type;
            data.Rarity = rarity;
            data.Source = source;
            data.Universe = universe;
            data.Faction = faction;
            data.Tags = tags;
            data.Keywords = keywords;

            data.Cost = cost;
            data.Attack = attack;
            data.Health = health;

            data.Actions = new List<CardActionData>(actions);

            return data;
        }

        private static CardData Unit(
            string id, string name,
            Universe universe, Faction faction, CardRarity rarity,
            int cost, int attack, int health,
            Keyword keywords, CardTag tags,
            params CardActionData[] actions)
        {
            return Create(id, name, CardType.Unit, universe, faction, rarity,
                cost, attack, health, keywords, tags, CardSource.Starter, actions);
        }

        // Token ใช้ overload นี้เพื่อกำหนด Source เอง
        private static CardData Unit(
            string id, string name,
            Universe universe, Faction faction, CardRarity rarity,
            int cost, int attack, int health,
            Keyword keywords, CardTag tags, CardSource source)
        {
            return Create(id, name, CardType.Unit, universe, faction, rarity,
                cost, attack, health, keywords, tags, source,
                new CardActionData[0]);
        }

        private static CardData Spell(
            string id, string name, CardRarity rarity, int cost,
            params CardActionData[] actions)
        {
            return Create(id, name, CardType.Spell,
                Universe.Original, Faction.Neutral, rarity,
                cost, 0, 0, Keyword.None, CardTag.Magic,
                CardSource.Starter, actions);
        }

        private static CardActionData Act(
            TriggerType trigger,
            TargetRule rule,
            TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = new CardActionData();

            action.Trigger = trigger;
            action.Target = rule;
            action.Selection = selection;
            action.Effects = new List<EffectData>(effects);

            return action;
        }

        private static EffectData Fx(EffectType type, int value = 0)
        {
            EffectData effect = new EffectData();

            effect.Type = type;
            effect.Value = value;

            return effect;
        }

        private static EffectData FxHeal(EffectType type, int value, int healOwnerPerHit)
        {
            EffectData effect = Fx(type, value);

            effect.HealOwnerPerHit = healOwnerPerHit;

            return effect;
        }

        private static EffectData FxCard(EffectType type, CardData card)
        {
            EffectData effect = Fx(type);

            effect.Card = card;

            return effect;
        }

        private static EffectData FxKeyword(EffectType type, Keyword keyword)
        {
            EffectData effect = Fx(type);

            effect.Keyword = keyword;

            return effect;
        }


        // Action ที่ทำงานเมื่อเข้าเงื่อนไข (เช่น Combo)
        private static CardActionData ActIf(
            ConditionType condition,
            int conditionValue,
            TriggerType trigger,
            TargetRule rule,
            TargetSelection selection,
            params EffectData[] effects)
        {
            CardActionData action = Act(trigger, rule, selection, effects);

            ConditionData data = new ConditionData();

            data.Condition = condition;
            data.Value = conditionValue;

            action.Conditions.Add(data);

            return action;
        }

        // ApplyStatus: Value = ความแรง, Duration 0 = ค่าเริ่มต้นของสถานะ
        private static EffectData FxApply(StatusType status, int value, int duration)
        {
            EffectData effect = Fx(EffectType.ApplyStatus, value);

            effect.Status = status;
            effect.Duration = duration;

            return effect;
        }

        // Transform ชั่วคราว (duration > 0) หรือถาวร (0)
        private static EffectData FxTransform(CardData card, int duration)
        {
            EffectData effect = FxCard(EffectType.Transform, card);

            effect.Duration = duration;

            return effect;
        }

        private static EffectData FxStatus(EffectType type, int value, int duration)
        {
            EffectData effect = Fx(type, value);

            effect.Duration = duration;

            return effect;
        }
    }
}
